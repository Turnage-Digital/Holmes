using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Holmes.Intake.Tests.TestHelpers;
using Holmes.Workflow.Application.Commands;
using MediatR;
using NSubstitute;
using Xunit;

namespace Holmes.Intake.Tests;

public class StartIntakeSessionCommandTests
{
    private readonly IIntakeSessionRepository _repository = Substitute.For<IIntakeSessionRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IIntakeUnitOfWork _unitOfWork = Substitute.For<IIntakeUnitOfWork>();

    public StartIntakeSessionCommandTests()
    {
        _unitOfWork.IntakeSessions.Returns(_repository);
    }

    [Fact]
    public async Task StartsSessionAndNotifiesWorkflow()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntakeSession?>(session));
        var handler = new StartIntakeSessionCommandHandler(_unitOfWork, _sender);

        var command = new StartIntakeSessionCommand(
            session.Id,
            session.ResumeToken,
            DateTimeOffset.UtcNow,
            "ios");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(IntakeSessionStatus.InProgress, session.Status);
        await _sender.Received(1)
            .Send(Arg.Is<MarkOrderIntakeStartedCommand>(c => c.OrderId == session.OrderId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectsInvalidResumeToken()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntakeSession?>(session));
        var handler = new StartIntakeSessionCommandHandler(_unitOfWork, _sender);

        var result = await handler.Handle(new StartIntakeSessionCommand(
            session.Id,
            "invalid",
            DateTimeOffset.UtcNow,
            null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(IntakeSessionStatus.Invited, session.Status);
    }
}