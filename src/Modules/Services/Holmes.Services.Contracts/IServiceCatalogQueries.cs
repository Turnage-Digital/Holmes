using Holmes.Services.Contracts.Dtos;

namespace Holmes.Services.Contracts;

/// <summary>
///     Query interface for service catalog lookups. Used by application layer for read operations.
/// </summary>
public interface IServiceCatalogQueries
{
    /// <summary>
    ///     Gets the service catalog for a customer, including all services and tier configurations.
    ///     Returns default catalog if no custom catalog exists.
    /// </summary>
    Task<CustomerServiceCatalogDto> GetByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets the raw catalog config for a customer, or null if no custom catalog exists.
    ///     Used for update operations that need the underlying config structure.
    /// </summary>
    Task<CatalogConfigDto?> GetConfigByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets the current version number of the customer's catalog.
    ///     Returns 0 if no catalog snapshot exists.
    /// </summary>
    Task<int> GetCurrentVersionAsync(
        string customerId,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     Internal catalog configuration structure for storage and updates.
/// </summary>
public sealed record CatalogConfigDto(
    IReadOnlyList<ServiceConfigDto> Services,
    IReadOnlyList<TierConfigDto> Tiers
);

/// <summary>
///     Individual service configuration.
/// </summary>
public sealed record ServiceConfigDto(
    string ServiceTypeCode,
    string DisplayName,
    string Category,
    bool IsEnabled,
    int Tier,
    string? VendorCode
);

/// <summary>
///     Tier configuration.
/// </summary>
public sealed record TierConfigDto(
    int Tier,
    string Name,
    string? Description,
    IReadOnlyCollection<string> RequiredServices,
    IReadOnlyCollection<string> OptionalServices,
    bool AutoDispatch,
    bool WaitForPreviousTier
);