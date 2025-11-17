using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Holmes.Intake.Tests.TestHelpers;
using NSubstitute;
using Xunit;

namespace Holmes.Intake.Tests;

public class VerifyIntakeSessionOtpCommandTests
{
    private readonly IIntakeSessionRepository _repository = Substitute.For<IIntakeSessionRepository>();
    private readonly IIntakeUnitOfWork _unitOfWork = Substitute.For<IIntakeUnitOfWork>();

    public VerifyIntakeSessionOtpCommandTests()
    {
        _unitOfWork.IntakeSessions.Returns(_repository);
    }

    [Fact]
    public async Task Succeeds_When_Code_Matches()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntakeSession?>(session));
        var handler = new VerifyIntakeSessionOtpCommandHandler(_unitOfWork);

        var result = await handler.Handle(
            new VerifyIntakeSessionOtpCommand(session.Id, session.ResumeToken),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Fails_When_Code_Does_Not_Match()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntakeSession?>(session));
        var handler = new VerifyIntakeSessionOtpCommandHandler(_unitOfWork);

        var result = await handler.Handle(
            new VerifyIntakeSessionOtpCommand(session.Id, "123456"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}