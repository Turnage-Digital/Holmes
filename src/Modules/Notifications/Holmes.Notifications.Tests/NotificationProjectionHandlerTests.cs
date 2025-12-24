using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Application.Abstractions;
using Holmes.Notifications.Application.EventHandlers;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.Events;
using Moq;

namespace Holmes.Notifications.Tests;

public sealed class NotificationProjectionHandlerTests
{
    private NotificationProjectionHandler _handler = null!;
    private Mock<INotificationProjectionWriter> _writerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _writerMock = new Mock<INotificationProjectionWriter>();
        _handler = new NotificationProjectionHandler(_writerMock.Object);
    }

    [Test]
    public async Task Handle_NotificationCreated_UpsertsProjection()
    {
        var notificationId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var subjectId = UlidId.NewUlid();
        var createdAt = DateTimeOffset.UtcNow;
        var scheduledFor = createdAt.AddMinutes(5);

        var notification = new NotificationCreated(
            notificationId,
            customerId,
            orderId,
            subjectId,
            NotificationTriggerType.IntakeSessionInvited,
            NotificationChannel.Email,
            false,
            createdAt,
            scheduledFor);

        NotificationProjectionModel? capturedModel = null;
        _writerMock
            .Setup(x => x.UpsertAsync(It.IsAny<NotificationProjectionModel>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationProjectionModel, CancellationToken>((m, _) => capturedModel = m)
            .Returns(Task.CompletedTask);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpsertAsync(It.IsAny<NotificationProjectionModel>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.That(capturedModel, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedModel!.NotificationId, Is.EqualTo(notificationId.ToString()));
            Assert.That(capturedModel.CustomerId, Is.EqualTo(customerId.ToString()));
            Assert.That(capturedModel.OrderId, Is.EqualTo(orderId.ToString()));
            Assert.That(capturedModel.SubjectId, Is.EqualTo(subjectId.ToString()));
            Assert.That(capturedModel.TriggerType, Is.EqualTo(NotificationTriggerType.IntakeSessionInvited));
            Assert.That(capturedModel.Channel, Is.EqualTo(NotificationChannel.Email));
            Assert.That(capturedModel.Status, Is.EqualTo(DeliveryStatus.Pending));
            Assert.That(capturedModel.IsAdverseAction, Is.False);
            Assert.That(capturedModel.CreatedAt, Is.EqualTo(createdAt));
            Assert.That(capturedModel.ScheduledFor, Is.EqualTo(scheduledFor));
        });
    }

    [Test]
    public async Task Handle_NotificationCreated_WithNullOptionalFields_UpsertsProjection()
    {
        var notificationId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var createdAt = DateTimeOffset.UtcNow;

        var notification = new NotificationCreated(
            notificationId,
            customerId,
            null,
            null,
            NotificationTriggerType.SlaClockAtRisk,
            NotificationChannel.Webhook,
            false,
            createdAt,
            null);

        NotificationProjectionModel? capturedModel = null;
        _writerMock
            .Setup(x => x.UpsertAsync(It.IsAny<NotificationProjectionModel>(), It.IsAny<CancellationToken>()))
            .Callback<NotificationProjectionModel, CancellationToken>((m, _) => capturedModel = m)
            .Returns(Task.CompletedTask);

        await _handler.Handle(notification, CancellationToken.None);

        Assert.That(capturedModel, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedModel!.OrderId, Is.Null);
            Assert.That(capturedModel.SubjectId, Is.Null);
            Assert.That(capturedModel.ScheduledFor, Is.Null);
        });
    }

    [Test]
    public async Task Handle_NotificationQueued_UpdatesQueuedInfo()
    {
        var notificationId = UlidId.NewUlid();
        var queuedAt = DateTimeOffset.UtcNow;

        var notification = new NotificationQueued(notificationId, queuedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateQueuedAsync(
                notificationId.ToString(),
                queuedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_NotificationDelivered_UpdatesDeliveredInfo()
    {
        var notificationId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var deliveredAt = DateTimeOffset.UtcNow;
        const string providerMessageId = "msg-12345";

        var notification = new NotificationDelivered(
            notificationId,
            customerId,
            orderId,
            NotificationChannel.Email,
            deliveredAt,
            providerMessageId);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateDeliveredAsync(
                notificationId.ToString(),
                deliveredAt,
                providerMessageId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_NotificationDelivered_WithNullProviderMessageId_UpdatesDeliveredInfo()
    {
        var notificationId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var deliveredAt = DateTimeOffset.UtcNow;

        var notification = new NotificationDelivered(
            notificationId,
            customerId,
            null,
            NotificationChannel.Sms,
            deliveredAt,
            null);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateDeliveredAsync(
                notificationId.ToString(),
                deliveredAt,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_NotificationDeliveryFailed_UpdatesFailedInfo()
    {
        var notificationId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var failedAt = DateTimeOffset.UtcNow;
        const string reason = "SMTP connection refused";
        const int attemptNumber = 3;

        var notification = new NotificationDeliveryFailed(
            notificationId,
            customerId,
            orderId,
            NotificationChannel.Email,
            failedAt,
            reason,
            attemptNumber);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateFailedAsync(
                notificationId.ToString(),
                failedAt,
                reason,
                attemptNumber,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_NotificationBounced_UpdatesBouncedInfo()
    {
        var notificationId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var bouncedAt = DateTimeOffset.UtcNow;
        const string reason = "Mailbox not found";

        var notification = new NotificationBounced(
            notificationId,
            customerId,
            orderId,
            bouncedAt,
            reason);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateBouncedAsync(
                notificationId.ToString(),
                bouncedAt,
                reason,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_NotificationCancelled_UpdatesCancelledInfo()
    {
        var notificationId = UlidId.NewUlid();
        var cancelledAt = DateTimeOffset.UtcNow;
        const string reason = "Order cancelled by customer";

        var notification = new NotificationCancelled(
            notificationId,
            cancelledAt,
            reason);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateCancelledAsync(
                notificationId.ToString(),
                cancelledAt,
                reason,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}