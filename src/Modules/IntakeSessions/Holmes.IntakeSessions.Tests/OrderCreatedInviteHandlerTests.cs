using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Application.EventHandlers;
using Holmes.Orders.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Holmes.IntakeSessions.Tests;

public class OrderCreatedInviteHandlerTests
{
    [Test]
    public async Task Uses_CreatedBy_As_Command_UserId()
    {
        var sender = new Mock<ISender>();
        IssueIntakeInviteCommand? captured = null;

        sender.Setup(s => s.Send(It.IsAny<IRequest<Result<IssueIntakeInviteResult>>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Result<IssueIntakeInviteResult>>, CancellationToken>((command, _) =>
            {
                captured = (IssueIntakeInviteCommand)command;
            })
            .ReturnsAsync(Result.Success(new IssueIntakeInviteResult(
                UlidId.NewUlid(),
                "resume-token",
                DateTimeOffset.UtcNow.AddDays(1))));

        var handler = new OrderCreatedInviteHandler(
            sender.Object,
            NullLogger<OrderCreatedInviteHandler>.Instance);

        var createdBy = UlidId.NewUlid();
        var notification = new OrderCreatedIntegrationEvent(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            "policy-v1",
            DateTimeOffset.UtcNow,
            createdBy);

        await handler.Handle(notification, CancellationToken.None);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.UserId, Is.EqualTo(createdBy.ToString()));
    }
}