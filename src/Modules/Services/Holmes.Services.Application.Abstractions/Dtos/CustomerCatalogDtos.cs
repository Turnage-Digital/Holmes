using Holmes.Services.Domain;

namespace Holmes.Services.Application.Abstractions.Dtos;

/// <summary>
///     A service item in a customer's catalog.
/// </summary>
public sealed record CatalogServiceItemDto(
    string ServiceTypeCode,
    string DisplayName,
    ServiceCategory Category,
    bool IsEnabled,
    int Tier,
    string? VendorCode
);

/// <summary>
///     Tier configuration within a customer catalog.
/// </summary>
public sealed record TierConfigurationDto(
    int Tier,
    string Name,
    string? Description,
    IReadOnlyCollection<string> RequiredServices,
    IReadOnlyCollection<string> OptionalServices,
    bool AutoDispatch,
    bool WaitForPreviousTier
);

/// <summary>
///     Full customer service catalog including all services and tier configurations.
/// </summary>
public sealed record CustomerServiceCatalogDto(
    string CustomerId,
    IReadOnlyCollection<CatalogServiceItemDto> Services,
    IReadOnlyCollection<TierConfigurationDto> Tiers,
    DateTimeOffset UpdatedAt
);