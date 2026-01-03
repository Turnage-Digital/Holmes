using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain.Events;
using Holmes.Notifications.Domain.ValueObjects;

namespace Holmes.Notifications.Domain;

public sealed class Notification : AggregateRoot
{
    private readonly List<DeliveryAttempt> _deliveryAttempts = [];

    private Notification()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId CustomerId { get; private set; }
    public UlidId? OrderId { get; private set; }
    public UlidId? SubjectId { get; private set; }
    public NotificationTriggerType TriggerType { get; private set; }
    public NotificationRecipient Recipient { get; private set; } = null!;
    public NotificationContent Content { get; private set; } = null!;
    public NotificationSchedule Schedule { get; private set; } = null!;
    public NotificationPriority Priority { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public bool IsAdverseAction { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ScheduledFor { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? CorrelationId { get; private set; }
    public IReadOnlyList<DeliveryAttempt> DeliveryAttempts => _deliveryAttempts.AsReadOnly();

    public static Notification Rehydrate(
        UlidId id,
        UlidId customerId,
        UlidId? orderId,
        UlidId? subjectId,
        NotificationTriggerType triggerType,
        NotificationRecipient recipient,
        NotificationContent content,
        NotificationSchedule schedule,
        NotificationPriority priority,
        DeliveryStatus status,
        bool isAdverseAction,
        DateTimeOffset createdAt,
        DateTimeOffset? scheduledFor,
        DateTimeOffset? processedAt,
        DateTimeOffset? deliveredAt,
        string? correlationId,
        IEnumerable<DeliveryAttempt> deliveryAttempts
    )
    {
        var request = new Notification
        {
            Id = id,
            CustomerId = customerId,
            OrderId = orderId,
            SubjectId = subjectId,
            TriggerType = triggerType,
            Recipient = recipient,
            Content = content,
            Schedule = schedule,
            Priority = priority,
            Status = status,
            IsAdverseAction = isAdverseAction,
            CreatedAt = createdAt,
            ScheduledFor = scheduledFor,
            ProcessedAt = processedAt,
            DeliveredAt = deliveredAt,
            CorrelationId = correlationId
        };
        request._deliveryAttempts.AddRange(deliveryAttempts);
        return request;
    }

    public static Notification Create(
        UlidId customerId,
        NotificationTrigger trigger,
        NotificationRecipient recipient,
        NotificationContent content,
        NotificationSchedule? schedule,
        NotificationPriority priority,
        bool isAdverseAction,
        DateTimeOffset createdAt,
        string? correlationId = null
    )
    {
        var effectiveSchedule = schedule ?? NotificationSchedule.Immediate();
        var scheduledFor = ComputeScheduledTime(effectiveSchedule, createdAt);

        var request = new Notification
        {
            Id = UlidId.NewUlid(),
            CustomerId = customerId,
            OrderId = trigger.OrderId,
            SubjectId = trigger.SubjectId,
            TriggerType = trigger.Type,
            Recipient = recipient,
            Content = content,
            Schedule = effectiveSchedule,
            Priority = priority,
            Status = DeliveryStatus.Pending,
            IsAdverseAction = isAdverseAction,
            CreatedAt = createdAt,
            ScheduledFor = scheduledFor,
            CorrelationId = correlationId
        };

        request.AddDomainEvent(new NotificationCreated(
            request.Id,
            request.CustomerId,
            request.OrderId,
            request.SubjectId,
            request.TriggerType,
            request.Recipient.Channel,
            request.IsAdverseAction,
            createdAt,
            scheduledFor));

        return request;
    }

    private static DateTimeOffset? ComputeScheduledTime(NotificationSchedule schedule, DateTimeOffset createdAt)
    {
        return schedule.Type switch
        {
            ScheduleType.Immediate => null, // Process immediately, no scheduled time
            ScheduleType.Delayed when schedule.Delay.HasValue => createdAt.Add(schedule.Delay.Value),
            ScheduleType.Daily when schedule.DailyAt.HasValue =>
                ComputeNextDailyTime(createdAt, schedule.DailyAt.Value),
            ScheduleType.Weekly when schedule is { DaysOfWeek: not null, DailyAt: not null } =>
                ComputeNextWeeklyTime(createdAt, schedule.DaysOfWeek, schedule.DailyAt.Value),
            ScheduleType.Batched when schedule.BatchWindow.HasValue => createdAt.Add(schedule.BatchWindow.Value),
            _ => null
        };
    }

    private static DateTimeOffset ComputeNextDailyTime(DateTimeOffset from, TimeOnly dailyAt)
    {
        var todayAt = new DateTimeOffset(
            from.Year, from.Month, from.Day,
            dailyAt.Hour, dailyAt.Minute, dailyAt.Second,
            from.Offset);

        return todayAt > from ? todayAt : todayAt.AddDays(1);
    }

    private static DateTimeOffset ComputeNextWeeklyTime(DateTimeOffset from, DayOfWeek[] daysOfWeek, TimeOnly dailyAt)
    {
        for (var i = 0; i < 8; i++)
        {
            var candidate = from.AddDays(i);
            if (!daysOfWeek.Contains(candidate.DayOfWeek))
            {
                continue;
            }

            var candidateAt = new DateTimeOffset(
                candidate.Year, candidate.Month, candidate.Day,
                dailyAt.Hour, dailyAt.Minute, dailyAt.Second,
                from.Offset);

            if (candidateAt > from)
            {
                return candidateAt;
            }
        }

        // Fallback: next week same day
        return from.AddDays(7);
    }

    public void MarkQueued(DateTimeOffset queuedAt)
    {
        EnsureCanQueue();

        Status = DeliveryStatus.Queued;
        ProcessedAt = queuedAt;

        AddDomainEvent(new NotificationQueued(Id, queuedAt));
    }

    public void RecordDeliverySuccess(
        DateTimeOffset deliveredAt,
        string? providerMessageId = null
    )
    {
        if (Status == DeliveryStatus.Delivered)
        {
            return;
        }

        var attempt = DeliveryAttempt.Success(
            Recipient.Channel,
            deliveredAt,
            _deliveryAttempts.Count + 1,
            providerMessageId);

        _deliveryAttempts.Add(attempt);
        Status = DeliveryStatus.Delivered;
        DeliveredAt = deliveredAt;

        AddDomainEvent(new NotificationDelivered(
            Id,
            CustomerId,
            OrderId,
            Recipient.Channel,
            deliveredAt,
            providerMessageId));
    }

    public void RecordDeliveryFailure(
        DateTimeOffset failedAt,
        string reason,
        TimeSpan? retryAfter = null
    )
    {
        if (Status == DeliveryStatus.Delivered)
        {
            throw new InvalidOperationException("Cannot record failure for delivered notification.");
        }

        var attempt = DeliveryAttempt.Failure(
            Recipient.Channel,
            failedAt,
            _deliveryAttempts.Count + 1,
            reason,
            retryAfter);

        _deliveryAttempts.Add(attempt);
        Status = DeliveryStatus.Failed;

        AddDomainEvent(new NotificationDeliveryFailed(
            Id,
            CustomerId,
            OrderId,
            Recipient.Channel,
            failedAt,
            reason,
            attempt.AttemptNumber));
    }

    public void RecordBounce(DateTimeOffset bouncedAt, string reason)
    {
        if (Status == DeliveryStatus.Delivered)
        {
            throw new InvalidOperationException("Cannot record bounce for delivered notification.");
        }

        var attempt = DeliveryAttempt.Bounced(
            Recipient.Channel,
            bouncedAt,
            _deliveryAttempts.Count + 1,
            reason);

        _deliveryAttempts.Add(attempt);
        Status = DeliveryStatus.Bounced;

        AddDomainEvent(new NotificationBounced(Id, CustomerId, OrderId, bouncedAt, reason));
    }

    public void Cancel(DateTimeOffset cancelledAt, string reason)
    {
        if (Status == DeliveryStatus.Delivered)
        {
            throw new InvalidOperationException("Cannot cancel delivered notification.");
        }

        Status = DeliveryStatus.Cancelled;

        AddDomainEvent(new NotificationCancelled(Id, cancelledAt, reason));
    }

    private void EnsureCanQueue()
    {
        if (Status != DeliveryStatus.Pending && Status != DeliveryStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Notification must be pending or failed to queue. Current status: {Status}");
        }
    }

    public override string GetStreamId()
    {
        return $"{GetStreamType()}:{Id}";
    }

    public override string GetStreamType()
    {
        return "Notification";
    }
}