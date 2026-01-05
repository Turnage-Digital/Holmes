using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Tests.TestHelpers;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class CaptureConsentArtifactCommandTests
{
    private InMemoryIntakeSessionRepository _repository = null!;
    private Mock<IConsentArtifactStore> _storeMock = null!;
    private Mock<IIntakeSessionsUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryIntakeSessionRepository();
        _storeMock = new Mock<IConsentArtifactStore>();
        _unitOfWorkMock = new Mock<IIntakeSessionsUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repository);
    }

    [Test]
    public async Task PersistsArtifactAndUpdatesSession()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        await _repository.AddAsync(session, CancellationToken.None);

        var descriptor = new ConsentArtifactDescriptor(
            UlidId.NewUlid(),
            "application/pdf",
            1024,
            "hash",
            "SHA256",
            "schema",
            DateTimeOffset.UtcNow);
        _storeMock.Setup(x =>
                x.SaveAsync(It.IsAny<ConsentArtifactWriteRequest>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(descriptor);

        var handler = new CaptureConsentArtifactCommandHandler(_storeMock.Object, _unitOfWorkMock.Object);
        var command = new CaptureConsentArtifactCommand(
            session.Id,
            "application/pdf",
            "schema",
            [1, 2, 3],
            DateTimeOffset.UtcNow);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(descriptor));
            Assert.That(session.ConsentArtifact, Is.Not.Null);
        });
        _storeMock.Verify(
            x => x.SaveAsync(It.IsAny<ConsentArtifactWriteRequest>(), It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class InMemoryIntakeSessionRepository : IIntakeSessionRepository
    {
        private readonly Dictionary<UlidId, IntakeSession> _sessions = new();

        public Task AddAsync(IntakeSession session, CancellationToken cancellationToken)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }

        public Task<IntakeSession?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
        {
            _sessions.TryGetValue(id, out var session);
            return Task.FromResult(session);
        }

        public Task<IntakeSession?> GetByOrderIdAsync(UlidId orderId, CancellationToken cancellationToken)
        {
            var session = _sessions.Values
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault(s => s.OrderId == orderId);
            return Task.FromResult(session);
        }

        public Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
