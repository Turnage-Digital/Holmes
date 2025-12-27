using Holmes.Core.Application.Abstractions.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Abstractions.Services;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Application.Gateways;
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
    private Mock<IOrderWorkflowGateway> _orderGatewayMock = null!;
    private InMemoryIntakeSessionRepository _repository = null!;
    private Mock<ISubjectDataGateway> _subjectGatewayMock = null!;
    private Mock<IIntakeSessionsUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _orderGatewayMock = new Mock<IOrderWorkflowGateway>();
        _subjectGatewayMock = new Mock<ISubjectDataGateway>();
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
            _orderGatewayMock.Object,
            _subjectGatewayMock.Object,
            _decryptorMock.Object,
            _encryptorMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<SubmitIntakeCommandHandler>.Instance
        );
    }

    [Test]
    public async Task BlocksSubmissionWhenPolicyFails()
    {
        var session = await SeedReadySessionAsync();
        _orderGatewayMock.Setup(x =>
                x.ValidateSubmissionAsync(It.IsAny<OrderIntakeSubmission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderPolicyCheckResult.Blocked("policy"));
        var handler = CreateHandler();

        var result = await handler.Handle(new SubmitIntakeCommand(session.Id, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("policy"));
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.InProgress));
        });
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderGatewayMock.Verify(x => x.NotifyIntakeSubmittedAsync(
            It.IsAny<OrderIntakeSubmission>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AdvancesWhenPolicyAllows()
    {
        var session = await SeedReadySessionAsync();
        _orderGatewayMock.Setup(x =>
                x.ValidateSubmissionAsync(It.IsAny<OrderIntakeSubmission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderPolicyCheckResult.Allowed());
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
        _orderGatewayMock.Verify(x => x.NotifyIntakeSubmittedAsync(
            It.Is<OrderIntakeSubmission>(s => s.IntakeSessionId == session.Id),
            submittedAt,
            It.IsAny<CancellationToken>()), Times.Once);
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

        public Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}