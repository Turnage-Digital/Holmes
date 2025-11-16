using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Domain.Events;
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

        Assert.Equal(orderId, order.Id);
        Assert.Equal(subjectId, order.SubjectId);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal("policy-v1", order.PolicySnapshotId);
        Assert.Equal(OrderStatus.Created, order.Status);
        var createdEvent = Assert.Single(order.DomainEvents);
        Assert.IsType<OrderStatusChanged>(createdEvent);
    }

    [Fact]
    public void RecordInviteMovesOrderToInvited()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        var timestamp = DateTimeOffset.UtcNow;

        order.RecordInvite(sessionId, timestamp);

        Assert.Equal(OrderStatus.Invited, order.Status);
        Assert.Equal(sessionId, order.ActiveIntakeSessionId);
        Assert.Equal(timestamp, order.InvitedAt);
    }

    [Fact]
    public void IntakeProgressRequiresActiveSession()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);

        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);
        Assert.Equal(OrderStatus.IntakeInProgress, order.Status);

        var otherSession = UlidId.NewUlid();
        Assert.Throws<InvalidOperationException>(() =>
            order.MarkIntakeInProgress(otherSession, DateTimeOffset.UtcNow));
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

        Assert.Equal(OrderStatus.IntakeComplete, order.Status);
        Assert.Equal(sessionId, order.LastCompletedIntakeSessionId);
        Assert.Equal(submittedAt, order.IntakeCompletedAt);
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

        Assert.Equal(OrderStatus.ReadyForRouting, order.Status);
        Assert.Equal(readyAt, order.ReadyForRoutingAt);
    }

    [Fact]
    public void BlockAndResumeRestoresPreviousStatus()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);

        order.Block("policy gate", DateTimeOffset.UtcNow);
        Assert.Equal(OrderStatus.Blocked, order.Status);

        order.ResumeFromBlock("gate cleared", DateTimeOffset.UtcNow);
        Assert.Equal(OrderStatus.IntakeInProgress, order.Status);
    }

    [Fact]
    public void CancelStopsOrder()
    {
        var order = CreateOrder();
        var canceledAt = DateTimeOffset.UtcNow;

        order.Cancel("duplicate request", canceledAt);

        Assert.Equal(OrderStatus.Canceled, order.Status);
        Assert.Equal(canceledAt, order.CanceledAt);
        Assert.Throws<InvalidOperationException>(() =>
            order.RecordInvite(UlidId.NewUlid(), DateTimeOffset.UtcNow));
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