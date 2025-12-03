using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Domain.Events;

namespace Holmes.Workflow.Tests;

public class OrderTests
{
    [Test]
    public void CreateInitializesOrder()
    {
        var orderId = UlidId.NewUlid();
        var subjectId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var createdAt = DateTimeOffset.UtcNow;

        var order = Order.Create(orderId, subjectId, customerId, "policy-v1", createdAt);

        Assert.Multiple(() =>
        {
            Assert.That(order.Id, Is.EqualTo(orderId));
            Assert.That(order.SubjectId, Is.EqualTo(subjectId));
            Assert.That(order.CustomerId, Is.EqualTo(customerId));
            Assert.That(order.PolicySnapshotId, Is.EqualTo("policy-v1"));
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Created));
        });
        var createdEvent = order.DomainEvents.Single();
        Assert.That(createdEvent, Is.TypeOf<OrderStatusChanged>());
    }

    [Test]
    public void RecordInviteMovesOrderToInvited()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        var timestamp = DateTimeOffset.UtcNow;

        order.RecordInvite(sessionId, timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Invited));
            Assert.That(order.ActiveIntakeSessionId, Is.EqualTo(sessionId));
            Assert.That(order.InvitedAt, Is.EqualTo(timestamp));
        });
    }

    [Test]
    public void IntakeProgressRequiresActiveSession()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);

        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.IntakeInProgress));

        var otherSession = UlidId.NewUlid();
        Assert.Throws<InvalidOperationException>(() =>
            order.MarkIntakeInProgress(otherSession, DateTimeOffset.UtcNow));
    }

    [Test]
    public void IntakeSubmissionAdvancesOrder()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);
        var submittedAt = DateTimeOffset.UtcNow;

        order.MarkIntakeSubmitted(sessionId, submittedAt);

        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(OrderStatus.IntakeComplete));
            Assert.That(order.LastCompletedIntakeSessionId, Is.EqualTo(sessionId));
            Assert.That(order.IntakeCompletedAt, Is.EqualTo(submittedAt));
        });
    }

    [Test]
    public void ReadyForRoutingRequiresCompletedIntake()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeSubmitted(sessionId, DateTimeOffset.UtcNow);
        var readyAt = DateTimeOffset.UtcNow;

        order.MarkReadyForRouting(readyAt);

        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(OrderStatus.ReadyForRouting));
            Assert.That(order.ReadyForRoutingAt, Is.EqualTo(readyAt));
        });
    }

    [Test]
    public void BlockAndResumeRestoresPreviousStatus()
    {
        var order = CreateOrder();
        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, DateTimeOffset.UtcNow);
        order.MarkIntakeInProgress(sessionId, DateTimeOffset.UtcNow);

        order.Block("policy gate", DateTimeOffset.UtcNow);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.Blocked));

        order.ResumeFromBlock("gate cleared", DateTimeOffset.UtcNow);
        Assert.That(order.Status, Is.EqualTo(OrderStatus.IntakeInProgress));
    }

    [Test]
    public void CancelStopsOrder()
    {
        var order = CreateOrder();
        var canceledAt = DateTimeOffset.UtcNow;

        order.Cancel("duplicate request", canceledAt);

        Assert.Multiple(() =>
        {
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Canceled));
            Assert.That(order.CanceledAt, Is.EqualTo(canceledAt));
        });
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