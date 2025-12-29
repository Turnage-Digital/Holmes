using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Tests.TestHelpers;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class StartIntakeSessionCommandTests
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
    public async Task StartsSession()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repositoryMock.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var handler = new StartIntakeSessionCommandHandler(_unitOfWorkMock.Object);

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
        // Note: Workflow notification now happens via IntakeSessionStartedIntegrationEvent
        // handled by Orders.Application.
    }

    [Test]
    public async Task RejectsInvalidResumeToken()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        _repositoryMock.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var handler = new StartIntakeSessionCommandHandler(_unitOfWorkMock.Object);

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
