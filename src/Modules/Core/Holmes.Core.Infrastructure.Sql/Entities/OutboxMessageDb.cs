namespace Holmes.Core.Infrastructure.Sql.Entities;

public class OutboxMessageDb
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public DateTime? AvailableAfter { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}