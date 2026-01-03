using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;

namespace Holmes.Notifications.Tests;

public sealed class NotificationTests
{
    [Test]
    public void Create_SetsInitialState()
    {
        var customerId = UlidId.NewUlid();
        var trigger = NotificationTrigger.OrderStateChanged(
            UlidId.NewUlid(),
            customerId,
            "Created",
            "Invited");
        var recipient = NotificationRecipient.Email("test@example.com", "Test User");
        var content = new NotificationContent { Subject = "Subject", Body = "Body" };
        var now = DateTimeOffset.UtcNow;

        var request = Notification.Create(
            customerId,
            trigger,
            recipient,
            content,
            null,
            NotificationPriority.Normal,
            false,
            now);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(DeliveryStatus.Pending));
            Assert.That(request.Recipient.Channel, Is.EqualTo(NotificationChannel.Email));
            Assert.That(request.Priority, Is.EqualTo(NotificationPriority.Normal));
            Assert.That(request.IsAdverseAction, Is.False);
            Assert.That(request.CreatedAt, Is.EqualTo(now));
        });
    }

    [Test]
    public void MarkQueued_TransitionsFromPending()
    {
        var request = CreateTestRequest();
        var now = DateTimeOffset.UtcNow;

        request.MarkQueued(now);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(DeliveryStatus.Queued));
            Assert.That(request.ProcessedAt, Is.EqualTo(now));
        });
    }

    [Test]
    public void RecordDeliverySuccess_TransitionsToDelivered()
    {
        var request = CreateTestRequest();
        var queuedAt = DateTimeOffset.UtcNow;
        var deliveredAt = queuedAt.AddSeconds(5);

        request.MarkQueued(queuedAt);
        request.RecordDeliverySuccess(deliveredAt, "msg-123");

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(DeliveryStatus.Delivered));
            Assert.That(request.DeliveredAt, Is.EqualTo(deliveredAt));
            Assert.That(request.DeliveryAttempts, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void RecordDeliveryFailure_RecordsAttempt()
    {
        var request = CreateTestRequest();
        var now = DateTimeOffset.UtcNow;

        request.MarkQueued(now);
        request.RecordDeliveryFailure(now.AddSeconds(1), "Connection timeout");

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(DeliveryStatus.Failed));
            Assert.That(request.DeliveryAttempts, Has.Count.EqualTo(1));
            Assert.That(request.DeliveryAttempts[0].FailureReason, Is.EqualTo("Connection timeout"));
        });
    }

    [Test]
    public void RecordBounce_TransitionsToBounced()
    {
        var request = CreateTestRequest();
        var now = DateTimeOffset.UtcNow;

        request.MarkQueued(now);
        request.RecordBounce(now.AddSeconds(1), "Invalid email address");

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(DeliveryStatus.Bounced));
            Assert.That(request.DeliveryAttempts, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Cancel_TransitionsToCancelled()
    {
        var request = CreateTestRequest();
        var now = DateTimeOffset.UtcNow;

        request.Cancel(now, "No longer needed");

        Assert.That(request.Status, Is.EqualTo(DeliveryStatus.Cancelled));
    }

    [Test]
    public void Cancel_ThrowsWhenDelivered()
    {
        var request = CreateTestRequest();
        var now = DateTimeOffset.UtcNow;

        request.MarkQueued(now);
        request.RecordDeliverySuccess(now.AddSeconds(1));

        Assert.Throws<InvalidOperationException>(() =>
            request.Cancel(now.AddSeconds(2), "Too late"));
    }

    private static Notification CreateTestRequest()
    {
        var customerId = UlidId.NewUlid();
        var trigger = NotificationTrigger.OrderStateChanged(
            UlidId.NewUlid(),
            customerId,
            "Created",
            "Invited");
        var recipient = NotificationRecipient.Email("test@example.com", "Test User");
        var content = new NotificationContent { Subject = "Subject", Body = "Body" };

        return Notification.Create(
            customerId,
            trigger,
            recipient,
            content,
            null,
            NotificationPriority.Normal,
            false,
            DateTimeOffset.UtcNow);
    }
}