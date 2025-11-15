using System.Collections.Generic;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.ValueObjects;
using Holmes.Workflow.Application.Commands;
using MediatR;
using NSubstitute;
using Xunit;

namespace Holmes.Intake.Tests;

public class IssueIntakeInviteCommandTests
{
    private readonly IIntakeUnitOfWork _unitOfWork = Substitute.For<IIntakeUnitOfWork>();
    private readonly IIntakeSessionRepository _repository = Substitute.For<IIntakeSessionRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();

    public IssueIntakeInviteCommandTests()
    {
        _unitOfWork.IntakeSessions.Returns(_repository);
    }

    [Fact]
    public async Task CreatesSessionAndNotifiesWorkflow()
    {
        var handler = new IssueIntakeInviteCommandHandler(_unitOfWork, _sender);
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

        Assert.True(result.IsSuccess);
        await _repository.Received(1).AddAsync(Arg.Any<IntakeSession>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(Arg.Is<RecordOrderInviteCommand>(c => c.OrderId == command.OrderId),
            Arg.Any<CancellationToken>());
    }
}
