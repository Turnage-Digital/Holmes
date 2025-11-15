using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Application.Gateways;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.ValueObjects;
using Holmes.Intake.Tests.TestHelpers;
using NSubstitute;
using Xunit;

namespace Holmes.Intake.Tests;

public class AcceptIntakeSubmissionCommandTests
{
    private readonly IOrderWorkflowGateway _gateway = Substitute.For<IOrderWorkflowGateway>();
    private readonly InMemoryIntakeSessionRepository _repository = new();
    private readonly IIntakeUnitOfWork _unitOfWork = Substitute.For<IIntakeUnitOfWork>();

    public AcceptIntakeSubmissionCommandTests()
    {
        _unitOfWork.IntakeSessions.Returns(_repository);
    }

    [Fact]
    public async Task AcceptsSubmissionAndNotifiesOrder()
    {
        var session = await SeedAwaitingReviewAsync();
        var handler = new AcceptIntakeSubmissionCommandHandler(_gateway, _unitOfWork);
        var acceptedAt = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new AcceptIntakeSubmissionCommand(session.Id, acceptedAt),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(IntakeSessionStatus.Submitted, session.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _gateway.Received(1)
            .NotifyIntakeAcceptedAsync(Arg.Is<OrderIntakeSubmission>(s => s.IntakeSessionId == session.Id),
                acceptedAt,
                Arg.Any<CancellationToken>());
    }

    private async Task<IntakeSession> SeedAwaitingReviewAsync()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        session.SaveProgress(IntakeAnswersSnapshot.Create("schema", "hash", "cipher", DateTimeOffset.UtcNow));
        var artifact = ConsentArtifactPointer.Create(UlidId.NewUlid(), "application/pdf", 1024, "hash", "SHA256",
            "schema", DateTimeOffset.UtcNow);
        session.CaptureConsent(artifact);
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

        public Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken)
        {
            _sessions[session.Id] = session;
            return Task.CompletedTask;
        }
    }
}
