namespace Holmes.Services.Infrastructure.Sql.Entities;

/// <summary>
///     Database entity for the service request read-model projection.
///     Populated by event handlers for fast query access.
/// </summary>
public class ServiceProjectionDb
{
    public string Id { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string ServiceTypeCode { get; set; } = null!;
    public int Category { get; set; }
    public int Status { get; set; }
    public int Tier { get; set; }
    public string? ScopeType { get; set; }
    public string? ScopeValue { get; set; }
    public string? VendorCode { get; set; }
    public string? VendorReferenceId { get; set; }
    public int? ResultStatus { get; set; }
    public int RecordCount { get; set; }
    public string? LastError { get; set; }
    public string? CancelReason { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}