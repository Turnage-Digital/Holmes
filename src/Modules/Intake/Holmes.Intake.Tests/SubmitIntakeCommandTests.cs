using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Application.Gateways;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.ValueObjects;
using Holmes.Intake.Tests.TestHelpers;
using NSubstitute;
using Xunit;

namespace Holmes.Intake.Tests;

public class SubmitIntakeCommandTests
{
    private readonly IOrderWorkflowGateway _gateway = Substitute.For<IOrderWorkflowGateway>();
    private readonly InMemoryIntakeSessionRepository _repository = new();
    private readonly IIntakeUnitOfWork _unitOfWork = Substitute.For<IIntakeUnitOfWork>();

    public SubmitIntakeCommandTests()
    {
        _unitOfWork.IntakeSessions.Returns(_repository);
    }

    [Fact]
    public async Task BlocksSubmissionWhenPolicyFails()
    {
        var session = await SeedReadySessionAsync();
        _gateway.ValidateSubmissionAsync(Arg.Any<OrderIntakeSubmission>(), Arg.Any<CancellationToken>())
            .Returns(OrderPolicyCheckResult.Blocked("policy"));
        var handler = new SubmitIntakeCommandHandler(_gateway, _unitOfWork);

        var result = await handler.Handle(new SubmitIntakeCommand(session.Id, DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("policy", result.Error);
        Assert.Equal(IntakeSessionStatus.InProgress, session.Status);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _gateway.DidNotReceive()
            .NotifyIntakeSubmittedAsync(Arg.Any<OrderIntakeSubmission>(), Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdvancesWhenPolicyAllows()
    {
        var session = await SeedReadySessionAsync();
        _gateway.ValidateSubmissionAsync(Arg.Any<OrderIntakeSubmission>(), Arg.Any<CancellationToken>())
            .Returns(OrderPolicyCheckResult.Allowed());
        var handler = new SubmitIntakeCommandHandler(_gateway, _unitOfWork);
        var submittedAt = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new SubmitIntakeCommand(session.Id, submittedAt),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(IntakeSessionStatus.AwaitingReview, session.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _gateway.Received(1)
            .NotifyIntakeSubmittedAsync(Arg.Is<OrderIntakeSubmission>(s => s.IntakeSessionId == session.Id),
                submittedAt,
                Arg.Any<CancellationToken>());
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