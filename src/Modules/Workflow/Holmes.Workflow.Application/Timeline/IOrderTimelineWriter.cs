using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Workflow.Application.Timeline;

public interface IOrderTimelineWriter
{
    Task WriteAsync(OrderTimelineEntry entry, CancellationToken cancellationToken);
}

public sealed record OrderTimelineEntry(
    UlidId OrderId,
    string EventType,
    string Description,
    string Source,
    DateTimeOffset OccurredAt,
    object? Metadata = null
);
