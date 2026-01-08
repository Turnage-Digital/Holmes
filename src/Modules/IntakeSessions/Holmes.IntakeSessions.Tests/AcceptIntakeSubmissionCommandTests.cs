using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using Holmes.IntakeSessions.Tests.TestHelpers;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class AcceptIntakeSubmissionCommandTests
{
    private InMemoryIntakeSessionRepository _repository = null!;
    private Mock<IIntakeSessionsUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryIntakeSessionRepository();
        _unitOfWorkMock = new Mock<IIntakeSessionsUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repository);
    }

    [Test]
    public async Task AcceptsSubmission()
    {
        var session = await SeedAwaitingReviewAsync();
        var handler = new AcceptIntakeSubmissionCommandHandler(_unitOfWorkMock.Object);
        var acceptedAt = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new AcceptIntakeSubmissionCommand(session.Id, acceptedAt),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.Submitted));
        });
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task<IntakeSession> SeedAwaitingReviewAsync()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        session.SaveProgress(IntakeAnswersSnapshot.Create("schema", "hash", "cipher", DateTimeOffset.UtcNow));
        var artifact = AuthorizationArtifactPointer.Create(UlidId.NewUlid(), "application/pdf", 1024, "hash",
            "SHA256",
            "schema", DateTimeOffset.UtcNow);
        session.CaptureAuthorization(artifact);
        session.Submit(DateTimeOffset.UtcNow);
        await _repository.AddAsync(session, CancellationToken.None);
        return session;
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
