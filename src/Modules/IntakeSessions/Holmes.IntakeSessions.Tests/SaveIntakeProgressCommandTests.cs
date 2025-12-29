using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Tests.TestHelpers;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class SaveIntakeProgressCommandTests
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
    public async Task SavesSnapshotWhenTokenMatches()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        _repositoryMock.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var handler = new SaveIntakeProgressCommandHandler(_unitOfWorkMock.Object);
        var updatedAt = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new SaveIntakeProgressCommand(
            session.Id,
            session.ResumeToken,
            "schema",
            "hash",
            "cipher",
            updatedAt), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(session.AnswersSnapshot, Is.Not.Null);
            Assert.That(session.AnswersSnapshot!.UpdatedAt, Is.EqualTo(updatedAt));
        });
    }

    [Test]
    public async Task RejectsInvalidToken()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        _repositoryMock.Setup(x => x.GetByIdAsync(session.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        var handler = new SaveIntakeProgressCommandHandler(_unitOfWorkMock.Object);

        var result = await handler.Handle(new SaveIntakeProgressCommand(
            session.Id,
            "oops",
            "schema",
            "hash",
            "cipher",
            DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(session.AnswersSnapshot, Is.Null);
        });
    }
}
