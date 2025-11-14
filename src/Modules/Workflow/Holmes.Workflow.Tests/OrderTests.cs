using FluentAssertions;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using Xunit;

namespace Holmes.Workflow.Tests;

public class OrderTests
{
    [Fact]
    public void CreateInitializesOrder()
    {
        var orderId = UlidId.NewUlid();
        var subjectId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var createdAt = DateTimeOffset.UtcNow;

        var order = Order.Create(orderId, subjectId, customerId, "policy-v1", createdAt);

        order.Id.Should().Be(orderId);
        order.SubjectId.Should().Be(subjectId);
        order.CustomerId.Should().Be(customerId);
        order.PolicySnapshotId.Should().Be("policy-v1");
        order.Status.Should().Be(OrderStatus.Created);
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<Holmes.Workflow.Domain.Events.OrderStatusChanged>();
    }

    [Fact]
    public void RecordInviteMovesOrderToInvited()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        var timestamp = DateTimeOffset.UtcNow;

        order.RecordInvite(sessionId, timestamp);

        order.Status.Should().Be(OrderStatus.Invited);
        order.ActiveIntakeSessionId.Should().Be(sessionId);
        order.InvitedAt.Should().Be(timestamp);
    }

    [Fact]
    public void IntakeProgressRequiresActiveSession()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);

        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);
        order.Status.Should().Be(OrderStatus.IntakeInProgress);

        var otherSession = UlidId.NewUlid();
        var action = () => order.MarkIntakeInProgress(otherSession, DateTimeOffset.UtcNow);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IntakeSubmissionAdvancesOrder()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);
        var submittedAt = DateTimeOffset.UtcNow;

        order.MarkIntakeSubmitted(sessionId, submittedAt);

        order.Status.Should().Be(OrderStatus.IntakeComplete);
        order.LastCompletedIntakeSessionId.Should().Be(sessionId);
        order.IntakeCompletedAt.Should().Be(submittedAt);
    }

    [Fact]
    public void ReadyForRoutingRequiresCompletedIntake()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeSubmitted(sessionId, DateTimeOffset.UtcNow);
        var readyAt = DateTimeOffset.UtcNow;

        order.MarkReadyForRouting(readyAt);

        order.Status.Should().Be(OrderStatus.ReadyForRouting);
        order.ReadyForRoutingAt.Should().Be(readyAt);
    }

    [Fact]
    public void BlockAndResumeRestoresPreviousStatus()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);

        order.Block("policy gate", DateTimeOffset.UtcNow);
        order.Status.Should().Be(OrderStatus.Blocked);

        order.ResumeFromBlock("gate cleared", DateTimeOffset.UtcNow);
        order.Status.Should().Be(OrderStatus.IntakeInProgress);
    }

    private static Order CreateOrder()
    {
        return Order.Create(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            "policy-v1",
            DateTimeOffset.UtcNow);
    }
}
