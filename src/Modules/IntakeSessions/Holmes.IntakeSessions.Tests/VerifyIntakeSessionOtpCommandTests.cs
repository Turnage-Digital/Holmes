using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Tests.TestHelpers;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class VerifyIntakeSessionOtpCommandTests
{
    private Mock<IIntakeSessionRepository> _repositoryMock = null!;
    private Mock<IIntakeSessionsUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IIntakeSessionRepository>();
        _unitOfWorkMock = new Mock<IIntakeSessionsUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repositoryMock.Object);
    }

    [Test]
    public async Task Succeeds_When_Code_Matches()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repositoryMock.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var handler = new VerifyIntakeSessionOtpCommandHandler(_unitOfWorkMock.Object);

        var result = await handler.Handle(
            new VerifyIntakeSessionOtpCommand(session.Id, session.ResumeToken),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task Fails_When_Code_Does_Not_Match()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repositoryMock.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var handler = new VerifyIntakeSessionOtpCommandHandler(_unitOfWorkMock.Object);

        var result = await handler.Handle(
            new VerifyIntakeSessionOtpCommand(session.Id, "123456"),
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.False);
    }
}
