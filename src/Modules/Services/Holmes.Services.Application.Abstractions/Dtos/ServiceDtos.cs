using Holmes.Services.Domain;

namespace Holmes.Services.Application.Abstractions.Dtos;

/// <summary>
/// Summary DTO for a service request, used in list views and order services.
/// </summary>
public sealed record ServiceRequestSummaryDto(
    string Id,
    string OrderId,
    string CustomerId,
    string ServiceTypeCode,
    ServiceCategory Category,
    int Tier,
    ServiceStatus Status,
    string? VendorCode,
    string? VendorReferenceId,
    int AttemptCount,
    int MaxAttempts,
    string? LastError,
    ServiceScopeType? ScopeType,
    string? ScopeValue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? FailedAt,
    DateTimeOffset? CanceledAt
);

/// <summary>
/// Response containing all services for an order with aggregated counts.
/// </summary>
public sealed record OrderServicesDto(
    string OrderId,
    IReadOnlyCollection<ServiceRequestSummaryDto> Services,
    int TotalServices,
    int CompletedServices,
    int PendingServices,
    int FailedServices
);

/// <summary>
/// DTO representing a service type definition.
/// </summary>
public sealed record ServiceTypeDto(
    string Code,
    string DisplayName,
    ServiceCategory Category,
    int DefaultTier
);
