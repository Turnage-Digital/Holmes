using System.Collections.Generic;
using System.Linq;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain.Events;

namespace Holmes.Workflow.Domain;

public sealed class Order : AggregateRoot
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> AllowedTransitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            { OrderStatus.Created, new[] { OrderStatus.Invited } },
            { OrderStatus.Invited, new[] { OrderStatus.IntakeInProgress } },
            { OrderStatus.IntakeInProgress, new[] { OrderStatus.IntakeComplete } },
            { OrderStatus.IntakeComplete, new[] { OrderStatus.ReadyForRouting } },
            { OrderStatus.ReadyForRouting, new[] { OrderStatus.RoutingInProgress } },
            { OrderStatus.RoutingInProgress, new[] { OrderStatus.ReadyForReport } },
            { OrderStatus.ReadyForReport, new[] { OrderStatus.Closed } },
            { OrderStatus.Closed, Array.Empty<OrderStatus>() },
            { OrderStatus.Blocked, Array.Empty<OrderStatus>() }
        };

    private Order()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId SubjectId { get; private set; }
    public UlidId CustomerId { get; private set; }
    public string PolicySnapshotId { get; private set; } = null!;
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
    public DateTimeOffset? ReadyForRoutingAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public static Order Create(
        UlidId orderId,
        UlidId subjectId,
        UlidId customerId,
        string policySnapshotId,
        DateTimeOffset createdAt,
        string? packageCode = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policySnapshotId);

        var order = new Order
        {
            Id = orderId,
            SubjectId = subjectId,
            CustomerId = customerId,
            PolicySnapshotId = policySnapshotId,
            PackageCode = packageCode,
            Status = OrderStatus.Created,
            CreatedAt = createdAt,
            LastUpdatedAt = createdAt
        };

        order.AddDomainEvent(new OrderStatusChanged(order.Id, order.Status, "Order created", createdAt));
        return order;
    }

    public static Order Rehydrate(
        UlidId orderId,
        UlidId subjectId,
        UlidId customerId,
        string policySnapshotId,
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
        DateTimeOffset? readyForRoutingAt,
        DateTimeOffset? closedAt
    )
    {
        return new Order
        {
            Id = orderId,
            SubjectId = subjectId,
            CustomerId = customerId,
            PolicySnapshotId = policySnapshotId,
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
            ReadyForRoutingAt = readyForRoutingAt,
            ClosedAt = closedAt
        };
    }

    public void RecordInvite(UlidId intakeSessionId, DateTimeOffset invitedAt, string? reason = null)
    {
        EnsureNotClosed();
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
        EnsureActiveSession(sessionId);
        reason ??= "Intake submission received";
        IntakeCompletedAt = submittedAt;
        LastCompletedIntakeSessionId = sessionId;
        TransitionTo(OrderStatus.IntakeComplete, reason, submittedAt);
    }

    public void MarkReadyForRouting(DateTimeOffset readyAt, string? reason = null)
    {
        EnsureNotClosed();
        if (Status != OrderStatus.IntakeComplete)
        {
            throw new InvalidOperationException("Order must be intake complete before it can be routed.");
        }

        if (LastCompletedIntakeSessionId is null)
        {
            throw new InvalidOperationException("No completed intake session is linked to this order.");
        }

        ReadyForRoutingAt = readyAt;
        reason ??= "Ready for routing";
        TransitionTo(OrderStatus.ReadyForRouting, reason, readyAt);
    }

    public void BeginRouting(DateTimeOffset startedAt, string? reason = null)
    {
        EnsureNotClosed();
        if (Status != OrderStatus.ReadyForRouting)
        {
            throw new InvalidOperationException("Routing can only begin from the ready for routing state.");
        }

        reason ??= "Routing started";
        TransitionTo(OrderStatus.RoutingInProgress, reason, startedAt);
    }

    public void MarkReadyForReport(DateTimeOffset readyAt, string? reason = null)
    {
        EnsureNotClosed();
        if (Status != OrderStatus.RoutingInProgress)
        {
            throw new InvalidOperationException("Order must be routing in progress before it can be ready for report.");
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
        AddDomainEvent(new OrderStatusChanged(Id, Status, reason, blockedAt));
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
        TransitionTo(target, reason, timestamp, allowAnyTransition: true);
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
        AddDomainEvent(new OrderStatusChanged(Id, newStatus, reason, timestamp));
    }

    private void EnsureActiveSession(UlidId sessionId)
    {
        EnsureNotClosed();

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

    private void EnsureNotClosed()
    {
        if (Status == OrderStatus.Closed)
        {
            throw new InvalidOperationException("Order is already closed.");
        }
    }
}
