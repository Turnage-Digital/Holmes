using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Moq;

namespace Holmes.Intake.Tests;

public class IssueIntakeInviteCommandTests
{
    private Mock<IIntakeSessionRepository> _repositoryMock = null!;
    private Mock<IIntakeUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void SetUp()
    {
        _repositoryMock = new Mock<IIntakeSessionRepository>();
        _unitOfWorkMock = new Mock<IIntakeUnitOfWork>();
        _unitOfWorkMock.Setup(x => x.IntakeSessions).Returns(_repositoryMock.Object);
    }

    [Test]
    public async Task CreatesSession()
    {
        var handler = new IssueIntakeInviteCommandHandler(_unitOfWorkMock.Object);
        var command = new IssueIntakeInviteCommand(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            "policy",
            "schema",
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            TimeSpan.FromHours(24),
            null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<IntakeSession>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Note: Workflow notification now happens via IntakeSessionInvited domain event
        // handled by IntakeToWorkflowHandler in App.Integration
    }
}