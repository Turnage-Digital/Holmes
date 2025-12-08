using Holmes.Services.Domain;

namespace Holmes.Services.Infrastructure.Sql.Entities;

public class ServiceRequestDb
{
    public string Id { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string? CatalogSnapshotId { get; set; }

    public string ServiceTypeCode { get; set; } = null!;
    public ServiceCategory Category { get; set; }
    public int Tier { get; set; }

    public ServiceScopeType? ScopeType { get; set; }
    public string? ScopeValue { get; set; }

    public ServiceStatus Status { get; set; }

    public string? VendorCode { get; set; }
    public string? VendorReferenceId { get; set; }

    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? CanceledAt { get; set; }

    // Result stored as related entity
    public ServiceResultDb? Result { get; set; }
}