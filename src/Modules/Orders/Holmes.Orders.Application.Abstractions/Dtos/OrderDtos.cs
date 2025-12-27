using System.Text.Json;

namespace Holmes.Orders.Application.Abstractions.Dtos;

public sealed record OrderSummaryDto(
    string OrderId,
    string SubjectId,
    string CustomerId,
    string PolicySnapshotId,
    string? PackageCode,
    string Status,
    string? LastStatusReason,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? ReadyForFulfillmentAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? CanceledAt
);

public sealed record OrderTimelineEntryDto(
    string EventId,
    string OrderId,
    string EventType,
    string Description,
    string Source,
    DateTimeOffset OccurredAt,
    DateTimeOffset RecordedAt,
    JsonElement? Metadata
);

public sealed record OrderStatsDto(
    int Invited,
    int IntakeInProgress,
    int IntakeComplete,
    int ReadyForFulfillment,
    int Blocked,
    int Canceled
);

/// <summary>
///     A raw domain event from the event store, used for audit purposes.
/// </summary>
public sealed record OrderAuditEventDto(
    long Position,
    long Version,
    string EventId,
    string EventName,
    JsonElement Payload,
    DateTimeOffset CreatedAt,
    string? CorrelationId,
    string? ActorId
);