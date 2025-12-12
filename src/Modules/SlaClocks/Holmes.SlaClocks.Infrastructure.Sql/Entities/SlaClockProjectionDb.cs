namespace Holmes.SlaClocks.Infrastructure.Sql.Entities;

/// <summary>
///     Database entity for the SLA clock read-model projection.
///     Populated by event handlers for fast query access.
/// </summary>
public class SlaClockProjectionDb
{
    public string Id { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public int Kind { get; set; }
    public int State { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime DeadlineAt { get; set; }
    public DateTime AtRiskThresholdAt { get; set; }
    public DateTime? AtRiskAt { get; set; }
    public DateTime? BreachedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? PauseReason { get; set; }
    public long AccumulatedPauseMs { get; set; }
    public int TargetBusinessDays { get; set; }
    public decimal AtRiskThresholdPercent { get; set; }
}
