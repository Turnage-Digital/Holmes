using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Application.Abstractions.Commands;

/// <summary>
///     Input for a service configuration in the catalog update.
/// </summary>
public sealed record ServiceCatalogServiceInput(
    string ServiceTypeCode,
    bool IsEnabled,
    int Tier,
    string? VendorCode
);

/// <summary>
///     Input for a tier configuration in the catalog update.
/// </summary>
public sealed record ServiceCatalogTierInput(
    int Tier,
    string Name,
    string? Description,
    IReadOnlyCollection<string> RequiredServices,
    IReadOnlyCollection<string> OptionalServices,
    bool AutoDispatch,
    bool WaitForPreviousTier
);

/// <summary>
///     Updates the entire service catalog configuration for a customer.
///     This is a cross-module command that persists to the Services module.
/// </summary>
public sealed record UpdateCustomerServiceCatalogCommand(
    string CustomerId,
    IReadOnlyCollection<ServiceCatalogServiceInput> Services,
    IReadOnlyCollection<ServiceCatalogTierInput> Tiers,
    UlidId UpdatedBy
) : RequestBase<Result>;