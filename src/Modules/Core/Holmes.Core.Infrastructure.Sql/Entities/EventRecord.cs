namespace Holmes.Core.Infrastructure.Sql.Entities;

public class EventRecord
{
    public long Position { get; set; }
    public string TenantId { get; set; } = null!;
    public string StreamId { get; set; } = null!;
    public string StreamType { get; set; } = null!;
    public long Version { get; set; }
    public string EventId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
    public string? ActorId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
}