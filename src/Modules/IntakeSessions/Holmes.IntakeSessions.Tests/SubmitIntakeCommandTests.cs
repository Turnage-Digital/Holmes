using Holmes.Core.Contracts.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Contracts.Services;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using Holmes.IntakeSessions.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class SubmitIntakeCommandTests
{
    private Mock<IIntakeAnswersDecryptor> _decryptorMock = null!;
    private Mock<IAeadEncryptor> _encryptorMock = null!;
    private InMemoryIntakeSessionRepository _repository = null!;
    private Mock<IIntakeSessionsUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _decryptorMock = new Mock<IIntakeAnswersDecryptor>();
        _encryptorMock = new Mock<IAeadEncryptor>();
        _repository = new InMemoryIntakeSessionRepository();
        _unitOfWorkMock = new Mock<IIntakeSessionsUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repository);

        // Default: decryptor returns null (no subject data to persist)
        _decryptorMock.Setup(x => x.DecryptAsync(It.IsAny<IntakeAnswersSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DecryptedIntakeAnswers?)null);
    }

    private SubmitIntakeCommandHandler CreateHandler()
    {
        return new SubmitIntakeCommandHandler(
            _decryptorMock.Object,
            _encryptorMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SubmitIntakeCommandHandler>.Instance
        );
    }

    [Test]
    public async Task AdvancesSubmission()
    {
        var session = await SeedReadySessionAsync();
        var handler = CreateHandler();
        var submittedAt = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new SubmitIntakeCommand(session.Id, submittedAt),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.AwaitingReview));
        });
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task<IntakeSession> SeedReadySessionAsync()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        session.SaveProgress(IntakeAnswersSnapshot.Create("schema", "hash", "cipher", DateTimeOffset.UtcNow));
        var artifact = ConsentArtifactPointer.Create(UlidId.NewUlid(), "application/pdf", 1024, "hash", "SHA256",
            "schema", DateTimeOffset.UtcNow);
        session.CaptureConsent(artifact);
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
