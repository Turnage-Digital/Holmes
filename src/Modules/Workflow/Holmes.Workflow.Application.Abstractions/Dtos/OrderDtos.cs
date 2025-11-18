using System.Text.Json;

namespace Holmes.Workflow.Application.Abstractions.Dtos;

public sealed record OrderSummaryDto(
    string OrderId,
    string SubjectId,
    string CustomerId,
    string PolicySnapshotId,
    string? PackageCode,
    string Status,
    string? LastStatusReason,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? ReadyForRoutingAt,
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