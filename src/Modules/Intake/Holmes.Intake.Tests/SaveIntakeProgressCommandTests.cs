using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Holmes.Intake.Tests.TestHelpers;
using NSubstitute;
using Xunit;

namespace Holmes.Intake.Tests;

public class SaveIntakeProgressCommandTests
{
    private readonly IIntakeUnitOfWork _unitOfWork = Substitute.For<IIntakeUnitOfWork>();
    private readonly IIntakeSessionRepository _repository = Substitute.For<IIntakeSessionRepository>();

    public SaveIntakeProgressCommandTests()
    {
        _unitOfWork.IntakeSessions.Returns(_repository);
    }

    [Fact]
    public async Task SavesSnapshotWhenTokenMatches()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntakeSession?>(session));
        var handler = new SaveIntakeProgressCommandHandler(_unitOfWork);
        var updatedAt = DateTimeOffset.UtcNow;

        var result = await handler.Handle(new SaveIntakeProgressCommand(
            session.Id,
            session.ResumeToken,
            "schema",
            "hash",
            "cipher",
            updatedAt), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(session.AnswersSnapshot);
        Assert.Equal(updatedAt, session.AnswersSnapshot!.UpdatedAt);
    }

    [Fact]
    public async Task RejectsInvalidToken()
    {
        var session = IntakeSessionTestFactory.CreateInvitedSession();
        session.Start(DateTimeOffset.UtcNow, null);
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IntakeSession?>(session));
        var handler = new SaveIntakeProgressCommandHandler(_unitOfWork);

        var result = await handler.Handle(new SaveIntakeProgressCommand(
            session.Id,
            "oops",
            "schema",
            "hash",
            "cipher",
            DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(session.AnswersSnapshot);
    }
}
