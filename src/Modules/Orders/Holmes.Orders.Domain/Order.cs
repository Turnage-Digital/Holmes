using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Orders.Domain.Events;

namespace Holmes.Orders.Domain;

public sealed class Order : AggregateRoot
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> AllowedTransitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.Created, [OrderStatus.Invited] },
            { OrderStatus.Invited, [OrderStatus.IntakeInProgress] },
            { OrderStatus.IntakeInProgress, [OrderStatus.IntakeComplete] },
            { OrderStatus.IntakeComplete, [OrderStatus.ReadyForFulfillment] },
            { OrderStatus.ReadyForFulfillment, [OrderStatus.FulfillmentInProgress] },
            { OrderStatus.FulfillmentInProgress, [OrderStatus.ReadyForReport] },
            { OrderStatus.ReadyForReport, [OrderStatus.Closed] },
            { OrderStatus.Closed, [] },
            { OrderStatus.Blocked, [] },
            { OrderStatus.Canceled, [] }
        };

    private Order()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId? SubjectId { get; private set; }
    public UlidId CustomerId { get; private set; }
    public string PolicySnapshotId { get; private set; } = null!;
    public string SubjectEmail { get; private set; } = string.Empty;
    public string? SubjectPhone { get; private set; }
    public string? PackageCode { get; private set; }
    public OrderStatus Status { get; private set; }
    public OrderStatus? BlockedFromStatus { get; private set; }
    public string? LastStatusReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }
    public UlidId? ActiveIntakeSessionId { get; private set; }
    public UlidId? LastCompletedIntakeSessionId { get; private set; }
    public DateTimeOffset? InvitedAt { get; private set; }
    public DateTimeOffset? IntakeStartedAt { get; private set; }
    public DateTimeOffset? IntakeCompletedAt { get; private set; }
    public DateTimeOffset? ReadyForFulfillmentAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }

    public static Order Create(
        UlidId orderId,
        UlidId customerId,
        string policySnapshotId,
        string subjectEmail,
        string? subjectPhone,
        DateTimeOffset createdAt,
        string? packageCode,
        UlidId createdBy
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySnapshotId);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectEmail);

        var normalizedEmail = subjectEmail.Trim().ToLowerInvariant();
        var normalizedPhone = string.IsNullOrWhiteSpace(subjectPhone) ? null : subjectPhone.Trim();

        var order = CreateCore(
            orderId,
            customerId,
            policySnapshotId,
            normalizedEmail,
            normalizedPhone,
            createdAt,
            packageCode);

        order.AddDomainEvent(new OrderRequested(
            order.Id,
            order.CustomerId,
            order.SubjectEmail,
            order.SubjectPhone,
            order.PolicySnapshotId,
            order.PackageCode,
            createdAt,
            createdBy));

        order.AddDomainEvent(new OrderStatusChanged(order.Id, order.CustomerId, order.Status, "Order created",
            createdAt));
        return order;
    }

    private static Order CreateCore(
        UlidId orderId,
        UlidId customerId,
        string policySnapshotId,
        string subjectEmail,
        string? subjectPhone,
        DateTimeOffset createdAt,
        string? packageCode
    )
    {
        return new Order
        {
            Id = orderId,
            SubjectId = null,
            CustomerId = customerId,
            PolicySnapshotId = policySnapshotId,
            SubjectEmail = subjectEmail,
            SubjectPhone = subjectPhone,
            PackageCode = packageCode,
            Status = OrderStatus.Created,
            CreatedAt = createdAt,
            LastUpdatedAt = createdAt
        };
    }

    public static Order Rehydrate(
        UlidId orderId,
        UlidId customerId,
        string policySnapshotId,
        string subjectEmail,
        string? subjectPhone,
        UlidId? subjectId,
        OrderStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset lastUpdatedAt,
        string? packageCode,
        string? lastStatusReason,
        OrderStatus? blockedFromStatus,
        UlidId? activeIntakeSessionId,
        UlidId? lastCompletedIntakeSessionId,
        DateTimeOffset? invitedAt,
        DateTimeOffset? intakeStartedAt,
        DateTimeOffset? intakeCompletedAt,
        DateTimeOffset? readyForFulfillmentAt,
        DateTimeOffset? closedAt,
        DateTimeOffset? canceledAt
    )
    {
        return new Order
        {
            Id = orderId,
            SubjectId = subjectId,
            CustomerId = customerId,
            PolicySnapshotId = policySnapshotId,
            SubjectEmail = subjectEmail,
            SubjectPhone = subjectPhone,
            Status = status,
            CreatedAt = createdAt,
            LastUpdatedAt = lastUpdatedAt,
            PackageCode = packageCode,
            LastStatusReason = lastStatusReason,
            BlockedFromStatus = blockedFromStatus,
            ActiveIntakeSessionId = activeIntakeSessionId,
            LastCompletedIntakeSessionId = lastCompletedIntakeSessionId,
            InvitedAt = invitedAt,
            IntakeStartedAt = intakeStartedAt,
            IntakeCompletedAt = intakeCompletedAt,
            ReadyForFulfillmentAt = readyForFulfillmentAt,
            ClosedAt = closedAt,
            CanceledAt = canceledAt
        };
    }

    public bool AssignSubject(UlidId subjectId, DateTimeOffset assignedAt)
    {
        if (SubjectId.HasValue)
        {
            return SubjectId.Value == subjectId;
        }

        SubjectId = subjectId;
        LastUpdatedAt = assignedAt;
        AddDomainEvent(new OrderSubjectAssigned(Id, CustomerId, subjectId, assignedAt));
        return true;
    }

    public bool LinkIntakeSession(UlidId intakeSessionId, DateTimeOffset linkedAt, string? reason = null)
    {
        if (ActiveIntakeSessionId.HasValue)
        {
            return ActiveIntakeSessionId.Value == intakeSessionId;
        }

        RecordInvite(intakeSessionId, linkedAt, reason ?? "Intake session linked");
        return true;
    }

    public void RecordInvite(UlidId intakeSessionId, DateTimeOffset invitedAt, string? reason = null)
    {
        EnsureNotFinalized();
        reason ??= "Invite issued";
        ActiveIntakeSessionId = intakeSessionId;
        InvitedAt = invitedAt;
        IntakeStartedAt = null;
        IntakeCompletedAt = null;
        LastCompletedIntakeSessionId = null;

        TransitionTo(OrderStatus.Invited, reason, invitedAt);
    }

    public void MarkIntakeInProgress(UlidId sessionId, DateTimeOffset startedAt, string? reason = null)
    {
        EnsureActiveSession(sessionId);
        reason ??= "Intake started";
        IntakeStartedAt = startedAt;
        TransitionTo(OrderStatus.IntakeInProgress, reason, startedAt);
    }

    public void MarkIntakeSubmitted(UlidId sessionId, DateTimeOffset submittedAt, string? reason = null)
    {
        if (Status == OrderStatus.IntakeComplete && LastCompletedIntakeSessionId == sessionId)
        {
            return;
        }

        EnsureActiveSession(sessionId);
        reason ??= "Intake submission received";
        IntakeCompletedAt = submittedAt;
        LastCompletedIntakeSessionId = sessionId;
        TransitionTo(OrderStatus.IntakeComplete, reason, submittedAt);
    }

    public void MarkReadyForFulfillment(DateTimeOffset readyAt, string? reason = null)
    {
        EnsureNotFinalized();
        if (Status != OrderStatus.IntakeComplete)
        {
            throw new InvalidOperationException("Order must be intake complete before fulfillment can begin.");
        }

        if (LastCompletedIntakeSessionId is null)
        {
            throw new InvalidOperationException("No completed intake session is linked to this order.");
        }

        ReadyForFulfillmentAt = readyAt;
        reason ??= "Ready for fulfillment";
        TransitionTo(OrderStatus.ReadyForFulfillment, reason, readyAt);
    }

    public void BeginFulfillment(DateTimeOffset startedAt, string? reason = null)
    {
        EnsureNotFinalized();
        if (Status != OrderStatus.ReadyForFulfillment)
        {
            throw new InvalidOperationException("Fulfillment can only begin from the ready for fulfillment state.");
        }

        reason ??= "Fulfillment in progress";
        TransitionTo(OrderStatus.FulfillmentInProgress, reason, startedAt);
    }

    public void MarkReadyForReport(DateTimeOffset readyAt, string? reason = null)
    {
        EnsureNotFinalized();
        if (Status != OrderStatus.FulfillmentInProgress)
        {
            throw new InvalidOperationException("Order must be in fulfillment before it can be ready for report.");
        }

        reason ??= "Ready for report";
        TransitionTo(OrderStatus.ReadyForReport, reason, readyAt);
    }

    public void Close(DateTimeOffset closedAt, string? reason = null)
    {
        if (Status == OrderStatus.Closed)
        {
            return;
        }

        if (Status != OrderStatus.ReadyForReport)
        {
            throw new InvalidOperationException("Order can only be closed once it is ready for report.");
        }

        ClosedAt = closedAt;
        reason ??= "Order closed";
        TransitionTo(OrderStatus.Closed, reason, closedAt);
    }

    public void Block(string reason, DateTimeOffset blockedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status == OrderStatus.Closed)
        {
            throw new InvalidOperationException("Closed orders cannot be blocked.");
        }

        if (Status == OrderStatus.Canceled)
        {
            throw new InvalidOperationException("Canceled orders cannot be blocked.");
        }

        if (Status == OrderStatus.Blocked)
        {
            LastStatusReason = reason;
            LastUpdatedAt = blockedAt;
            return;
        }

        BlockedFromStatus = Status;
        Status = OrderStatus.Blocked;
        LastStatusReason = reason;
        LastUpdatedAt = blockedAt;
        AddDomainEvent(new OrderStatusChanged(Id, CustomerId, Status, reason, blockedAt));
    }

    public void ResumeFromBlock(string reason, DateTimeOffset timestamp)
    {
        if (Status != OrderStatus.Blocked)
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (BlockedFromStatus is null)
        {
            throw new InvalidOperationException("Order has no previous status to return to.");
        }

        var target = BlockedFromStatus.Value;
        BlockedFromStatus = null;
        TransitionTo(target, reason, timestamp, true);
    }

    public void Cancel(string reason, DateTimeOffset canceledAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == OrderStatus.Canceled)
        {
            return;
        }

        if (Status == OrderStatus.Closed)
        {
            throw new InvalidOperationException("Closed orders cannot be canceled.");
        }

        CanceledAt = canceledAt;
        BlockedFromStatus = null;
        TransitionTo(OrderStatus.Canceled, reason, canceledAt, true);
    }

    private void TransitionTo(
        OrderStatus newStatus,
        string reason,
        DateTimeOffset timestamp,
        bool allowAnyTransition = false
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status == newStatus)
        {
            LastStatusReason = reason;
            LastUpdatedAt = timestamp;
            return;
        }

        if (Status == OrderStatus.Blocked && !allowAnyTransition)
        {
            throw new InvalidOperationException("Blocked orders must be resumed before transitioning.");
        }

        if (!allowAnyTransition &&
            (!AllowedTransitions.TryGetValue(Status, out var options) || !options.Contains(newStatus)))
        {
            throw new InvalidOperationException($"Cannot transition order from {Status} to {newStatus}.");
        }

        Status = newStatus;
        LastStatusReason = reason;
        LastUpdatedAt = timestamp;
        AddDomainEvent(new OrderStatusChanged(Id, CustomerId, newStatus, reason, timestamp));
    }

    private void EnsureActiveSession(UlidId sessionId)
    {
        EnsureNotFinalized();

        if (Status == OrderStatus.Blocked)
        {
            throw new InvalidOperationException("Blocked orders cannot progress.");
        }

        if (ActiveIntakeSessionId is null)
        {
            throw new InvalidOperationException("Order has no active intake session.");
        }

        if (ActiveIntakeSessionId != sessionId)
        {
            throw new InvalidOperationException("Intake session does not match the active session for this order.");
        }
    }

    private void EnsureNotFinalized()
    {
        if (Status is OrderStatus.Closed or OrderStatus.Canceled)
        {
            throw new InvalidOperationException("Order is no longer active.");
        }
    }

    public override string GetStreamId()
    {
        return $"{GetStreamType()}:{Id}";
    }

    public override string GetStreamType()
    {
        return "Order";
    }
}
