namespace Holmes.Core.Infrastructure.Sql.Entities;

public class SnapshotRecord
{
    public long Id { get; set; }
    public string TenantId { get; set; } = null!;
    public string StreamId { get; set; } = null!;
    public string StreamType { get; set; } = null!;
    public long Version { get; set; }
    public string Payload { get; set; } = null!;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Checksum { get; set; }
}