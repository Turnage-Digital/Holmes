using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Application.Gateways;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.ValueObjects;
using Holmes.Intake.Tests.TestHelpers;
using Moq;

namespace Holmes.Intake.Tests;

public class SubmitIntakeCommandTests
{
    private Mock<IOrderWorkflowGateway> _gatewayMock = null!;
    private InMemoryIntakeSessionRepository _repository = null!;
    private Mock<IIntakeUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _gatewayMock = new Mock<IOrderWorkflowGateway>();
        _repository = new InMemoryIntakeSessionRepository();
        _unitOfWorkMock = new Mock<IIntakeUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repository);
    }

    [Test]
    public async Task BlocksSubmissionWhenPolicyFails()
    {
        var session = await SeedReadySessionAsync();
        _gatewayMock.Setup(x => x.ValidateSubmissionAsync(It.IsAny<OrderIntakeSubmission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderPolicyCheckResult.Blocked("policy"));
        var handler = new SubmitIntakeCommandHandler(_gatewayMock.Object, _unitOfWorkMock.Object);

        var result = await handler.Handle(new SubmitIntakeCommand(session.Id, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo("policy"));
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.InProgress));
        });
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _gatewayMock.Verify(x => x.NotifyIntakeSubmittedAsync(
            It.IsAny<OrderIntakeSubmission>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AdvancesWhenPolicyAllows()
    {
        var session = await SeedReadySessionAsync();
        _gatewayMock.Setup(x => x.ValidateSubmissionAsync(It.IsAny<OrderIntakeSubmission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderPolicyCheckResult.Allowed());
        var handler = new SubmitIntakeCommandHandler(_gatewayMock.Object, _unitOfWorkMock.Object);
        var submittedAt = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new SubmitIntakeCommand(session.Id, submittedAt),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.AwaitingReview));
        });
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _gatewayMock.Verify(x => x.NotifyIntakeSubmittedAsync(
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