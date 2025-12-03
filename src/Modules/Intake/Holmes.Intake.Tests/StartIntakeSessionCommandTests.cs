using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Holmes.Intake.Tests.TestHelpers;
using Holmes.Workflow.Application.Commands;
using MediatR;
using Moq;

namespace Holmes.Intake.Tests;

public class StartIntakeSessionCommandTests
{
    private Mock<IIntakeSessionRepository> _repositoryMock = null!;
    private Mock<ISender> _senderMock = null!;
    private Mock<IIntakeUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IIntakeSessionRepository>();
        _senderMock = new Mock<ISender>();
        _unitOfWorkMock = new Mock<IIntakeUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repositoryMock.Object);
    }

    [Test]
    public async Task StartsSessionAndNotifiesWorkflow()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repositoryMock.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var handler = new StartIntakeSessionCommandHandler(_unitOfWorkMock.Object, _senderMock.Object);

        var command = new StartIntakeSessionCommand(
            session.Id,
            session.ResumeToken,
            DateTimeOffset.UtcNow,
            "ios");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.InProgress));
        });
        _senderMock.Verify(x => x.Send(
            It.Is<MarkOrderIntakeStartedCommand>(c => c.OrderId == session.OrderId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RejectsInvalidResumeToken()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repositoryMock.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var handler = new StartIntakeSessionCommandHandler(_unitOfWorkMock.Object, _senderMock.Object);

        var result = await handler.Handle(new StartIntakeSessionCommand(
            session.Id,
            "invalid",
            DateTimeOffset.UtcNow,
            null), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(session.Status, Is.EqualTo(IntakeSessionStatus.Invited));
        });
    }
}