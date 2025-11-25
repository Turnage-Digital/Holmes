using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Entities;
using Holmes.Core.Infrastructure.Sql.Projections;
using Holmes.Intake.Application.Abstractions.Sessions;
using Holmes.Intake.Domain;
using Holmes.Workflow.Application.Abstractions.Projections;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Workflow.Infrastructure.Sql.Projections;

public sealed class OrderTimelineProjectionRunner(
    WorkflowDbContext workflowDbContext,
    CoreDbContext coreDbContext,
    IIntakeSessionReplaySource intakeSessionReplaySource,
    IOrderTimelineWriter timelineWriter,
    ILogger<OrderTimelineProjectionRunner> logger
)
{
    private const string ProjectionName = "workflow.order_timeline";

    public async Task<ProjectionReplayResult> RunAsync(bool reset, CancellationToken cancellationToken)
    {
        if (!reset)
        {
            throw new InvalidOperationException(
                "Order timeline replay requires --reset true to avoid duplicating events.");
        }

        await ResetStateAsync(cancellationToken);

        var events = new List<TimelineReplayEvent>();

        var orders = await workflowDbContext.Orders
            .AsNoTracking()
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            events.AddRange(BuildOrderEvents(order));
        }

        var intakeSessions = await intakeSessionReplaySource.ListSessionsAsync(cancellationToken);
        foreach (var session in intakeSessions
                     .OrderBy(s => s.CreatedAt)
                     .ThenBy(s => s.IntakeSessionId))
        {
            events.AddRange(BuildIntakeEvents(session));
        }

        events.Sort(static (a, b) =>
        {
            var compare = a.OccurredAt.CompareTo(b.OccurredAt);
            if (compare != 0)
            {
                return compare;
            }

            compare = string.CompareOrdinal(a.EventType, b.EventType);
            if (compare != 0)
            {
                return compare;
            }

            return string.CompareOrdinal(a.OrderId.ToString(), b.OrderId.ToString());
        });

        foreach (var replayEvent in events)
        {
            var entry = new OrderTimelineEntry(
                replayEvent.OrderId,
                replayEvent.EventType,
                replayEvent.Description,
                replayEvent.Source,
                replayEvent.OccurredAt,
                replayEvent.Metadata);
            await timelineWriter.WriteAsync(entry, cancellationToken);
        }

        var last = events.LastOrDefault();
        await SaveCheckpointAsync(events.Count, last, cancellationToken);

        logger.LogInformation(
            "Order timeline replay wrote {Count} events. Last event occurred at {Timestamp} for order {OrderId}",
            events.Count,
            last?.OccurredAt.ToString("O") ?? "(n/a)",
            last?.OrderId.ToString() ?? "(none)");

        return new ProjectionReplayResult(
            events.Count,
            last?.OccurredAt,
            last?.OrderId.ToString());
    }

    private async Task ResetStateAsync(CancellationToken cancellationToken)
    {
        if (workflowDbContext.Database.IsRelational())
        {
            await workflowDbContext.OrderTimelineEvents.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            workflowDbContext.OrderTimelineEvents.RemoveRange(workflowDbContext.OrderTimelineEvents);
            await workflowDbContext.SaveChangesAsync(cancellationToken);
        }

        workflowDbContext.ChangeTracker.Clear();

        var checkpoint = await coreDbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName && x.TenantId == "*", cancellationToken);

        if (checkpoint is not null)
        {
            coreDbContext.ProjectionCheckpoints.Remove(checkpoint);
            await coreDbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SaveCheckpointAsync(
        int processed,
        TimelineReplayEvent? last,
        CancellationToken cancellationToken
    )
    {
        var checkpoint = await coreDbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName && x.TenantId == "*", cancellationToken);

        if (checkpoint is null)
        {
            checkpoint = new ProjectionCheckpoint
            {
                ProjectionName = ProjectionName,
                TenantId = "*"
            };
            coreDbContext.ProjectionCheckpoints.Add(checkpoint);
        }

        checkpoint.Position = processed;
        checkpoint.Cursor = last is null
            ? null
            : $"{last.OccurredAt:O}|{last.OrderId}";
        checkpoint.UpdatedAt = DateTime.UtcNow;
        await coreDbContext.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<TimelineReplayEvent> BuildOrderEvents(OrderDb order)
    {
        var orderId = UlidId.Parse(order.OrderId);
        var events = new List<TimelineReplayEvent>
        {
            new(orderId,
                order.CreatedAt,
                "order.status_changed",
                "Order created (replayed)",
                "workflow-replay",
                new { status = OrderStatus.Created.ToString() })
        };

        AddStatusEvent(events, orderId, order.InvitedAt, OrderStatus.Invited);
        AddStatusEvent(events, orderId, order.IntakeStartedAt, OrderStatus.IntakeInProgress);
        AddStatusEvent(events, orderId, order.IntakeCompletedAt, OrderStatus.IntakeComplete);
        AddStatusEvent(events, orderId, order.ReadyForRoutingAt, OrderStatus.ReadyForRouting);
        AddStatusEvent(events, orderId, order.ClosedAt, OrderStatus.Closed);
        AddStatusEvent(events, orderId, order.CanceledAt, OrderStatus.Canceled);

        return events;
    }

    private static void AddStatusEvent(
        ICollection<TimelineReplayEvent> events,
        UlidId orderId,
        DateTimeOffset? occurredAt,
        OrderStatus status
    )
    {
        if (occurredAt is null)
        {
            return;
        }

        events.Add(new TimelineReplayEvent(
            orderId,
            occurredAt.Value,
            "order.status_changed",
            $"Order status set to {status} (replayed)",
            "workflow-replay",
            new { status = status.ToString() }));
    }

    private static IEnumerable<TimelineReplayEvent> BuildIntakeEvents(IntakeSessionReplayRecord session)
    {
        var orderId = session.OrderId;
        var events = new List<TimelineReplayEvent>
        {
            new(orderId,
                session.CreatedAt,
                "intake.session_invited",
                "Intake invite issued (replayed)",
                "intake-replay",
                new
                {
                    sessionId = session.IntakeSessionId.ToString(),
                    expiresAt = session.ExpiresAt
                })
        };

        if (session.ConsentCapturedAt is not null && session.ConsentArtifactId is not null)
        {
            events.Add(new TimelineReplayEvent(
                orderId,
                session.ConsentCapturedAt.Value,
                "intake.consent_captured",
                "Consent captured (replayed)",
                "intake-replay",
                new
                {
                    sessionId = session.IntakeSessionId.ToString(),
                    artifactId = session.ConsentArtifactId
                }));
        }

        if (session.SubmittedAt is not null)
        {
            events.Add(new TimelineReplayEvent(
                orderId,
                session.SubmittedAt.Value,
                "intake.submission_received",
                "Intake submitted (replayed)",
                "intake-replay",
                new { sessionId = session.IntakeSessionId.ToString() }));
        }

        if (session.AcceptedAt is not null)
        {
            events.Add(new TimelineReplayEvent(
                orderId,
                session.AcceptedAt.Value,
                "intake.submission_accepted",
                "Intake accepted (replayed)",
                "intake-replay",
                new { sessionId = session.IntakeSessionId.ToString() }));
        }

        if (!string.IsNullOrWhiteSpace(session.CancellationReason) &&
            session.Status == IntakeSessionStatus.Abandoned)
        {
            events.Add(new TimelineReplayEvent(
                orderId,
                session.LastTouchedAt,
                "intake.session_abandoned",
                $"Session abandoned: {session.CancellationReason}",
                "intake-replay",
                new { sessionId = session.IntakeSessionId.ToString() }));
        }

        if (session.SupersededBySessionId is not null)
        {
            events.Add(new TimelineReplayEvent(
                orderId,
                session.LastTouchedAt,
                "intake.session_superseded",
                "Session superseded (replayed)",
                "intake-replay",
                new
                {
                    sessionId = session.IntakeSessionId.ToString(),
                    supersededBy = session.SupersededBySessionId.ToString()
                }));
        }

        return events;
    }

    private sealed record TimelineReplayEvent(
        UlidId OrderId,
        DateTimeOffset OccurredAt,
        string EventType,
        string Description,
        string Source,
        object? Metadata
    );
}