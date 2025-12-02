using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Holmes.Workflow.Application.Commands;
using MediatR;
using Moq;

namespace Holmes.Intake.Tests;

public class IssueIntakeInviteCommandTests
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
    public async Task CreatesSessionAndNotifiesWorkflow()
    {
        var handler = new IssueIntakeInviteCommandHandler(_unitOfWorkMock.Object, _senderMock.Object);
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
        _senderMock.Verify(x => x.Send(
            It.Is<RecordOrderInviteCommand>(c => c.OrderId == command.OrderId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}