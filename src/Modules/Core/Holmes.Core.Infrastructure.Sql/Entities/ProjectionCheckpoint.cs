using System;

namespace Holmes.Core.Infrastructure.Sql.Entities;

public class ProjectionCheckpoint
{
    public string ProjectionName { get; set; } = null!;
    public string TenantId { get; set; } = "*";
    public long Position { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Cursor { get; set; }
}
