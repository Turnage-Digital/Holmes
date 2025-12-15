using System.Text.Json;
using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

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

public sealed class UpdateCustomerServiceCatalogCommandHandler(
    IServiceCatalogQueries catalogQueries,
    IServiceCatalogRepository catalogRepository
) : IRequestHandler<UpdateCustomerServiceCatalogCommand, Result>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<Result> Handle(
        UpdateCustomerServiceCatalogCommand request,
        CancellationToken cancellationToken
    )
    {
        // Validate that at least some configuration is provided
        if (request.Services.Count == 0 && request.Tiers.Count == 0)
        {
            return Result.Fail("At least one service or tier configuration must be provided.");
        }

        // Get existing config to merge with or use as base
        var existingConfig = await catalogQueries.GetConfigByCustomerIdAsync(
            request.CustomerId, cancellationToken);

        // Build the new catalog config
        var services = BuildServicesConfig(request.Services, existingConfig);
        var tiers = BuildTiersConfig(request.Tiers, existingConfig);

        var updatedConfig = new CatalogConfigDto(services, tiers);

        // Get current version and save new snapshot
        var currentVersion = await catalogQueries.GetCurrentVersionAsync(
            request.CustomerId, cancellationToken);

        await catalogRepository.SaveSnapshotAsync(
            request.CustomerId,
            currentVersion + 1,
            JsonSerializer.Serialize(updatedConfig, JsonOptions),
            request.UpdatedBy.ToString(),
            cancellationToken);

        return Result.Success();
    }

    private static IReadOnlyList<ServiceConfigDto> BuildServicesConfig(
        IReadOnlyCollection<ServiceCatalogServiceInput> inputServices,
        CatalogConfigDto? existingConfig
    )
    {
        var existingServices = existingConfig?.Services ?? BuildDefaultServices();

        // Create a dictionary of existing services for lookup
        var servicesDict = existingServices.ToDictionary(
            s => s.ServiceTypeCode,
            s => s,
            StringComparer.OrdinalIgnoreCase);

        // Update services based on input
        foreach (var input in inputServices)
        {
            if (servicesDict.TryGetValue(input.ServiceTypeCode, out var existing))
            {
                servicesDict[input.ServiceTypeCode] = existing with
                {
                    IsEnabled = input.IsEnabled,
                    Tier = input.Tier,
                    VendorCode = input.VendorCode ?? existing.VendorCode
                };
            }
        }

        return servicesDict.Values.ToList();
    }

    private static IReadOnlyList<TierConfigDto> BuildTiersConfig(
        IReadOnlyCollection<ServiceCatalogTierInput> inputTiers,
        CatalogConfigDto? existingConfig
    )
    {
        var existingTiers = existingConfig?.Tiers ?? BuildDefaultTiers();

        // Create a dictionary of existing tiers for lookup
        var tiersDict = existingTiers.ToDictionary(t => t.Tier, t => t);

        // Update tiers based on input
        foreach (var input in inputTiers)
        {
            tiersDict[input.Tier] = new TierConfigDto(
                input.Tier,
                input.Name,
                input.Description,
                input.RequiredServices,
                input.OptionalServices,
                input.AutoDispatch,
                input.WaitForPreviousTier
            );
        }

        return tiersDict.Values.OrderBy(t => t.Tier).ToList();
    }

    private static IReadOnlyList<ServiceConfigDto> BuildDefaultServices()
    {
        return
        [
            new ServiceConfigDto("SSN_TRACE", "SSN Trace", "Identity", true, 1, null),
            new ServiceConfigDto("NATL_CRIM", "National Criminal Search", "Criminal", true, 1, null),
            new ServiceConfigDto("STATE_CRIM", "State Criminal Search", "Criminal", true, 2, null),
            new ServiceConfigDto("COUNTY_CRIM", "County Criminal Search", "Criminal", true, 2, null),
            new ServiceConfigDto("FED_CRIM", "Federal Criminal Search", "Criminal", true, 2, null),
            new ServiceConfigDto("SEX_OFFENDER", "Sex Offender Registry", "Criminal", true, 1, null),
            new ServiceConfigDto("GLOBAL_WATCH", "Global Watchlist Search", "Criminal", true, 1, null),
            new ServiceConfigDto("MVR", "Motor Vehicle Report", "Driving", true, 2, null),
            new ServiceConfigDto("CREDIT_CHECK", "Credit Report", "Credit", false, 3, null),
            new ServiceConfigDto("TWN_EMP", "Employment Verification (TWN)", "Employment", true, 3, null),
            new ServiceConfigDto("MANUAL_EMP", "Employment Verification (Manual)", "Employment", false, 3, null),
            new ServiceConfigDto("EDU_VERIFY", "Education Verification", "Education", true, 3, null),
            new ServiceConfigDto("PRO_LICENSE", "Professional License Verification", "Employment", false, 3, null),
            new ServiceConfigDto("REF_CHECK", "Reference Check", "Reference", false, 4, null),
            new ServiceConfigDto("DRUG_5", "5-Panel Drug Screen", "Drug", false, 4, null),
            new ServiceConfigDto("DRUG_10", "10-Panel Drug Screen", "Drug", false, 4, null),
            new ServiceConfigDto("CIVIL_COURT", "Civil Court Search", "Civil", false, 2, null),
            new ServiceConfigDto("BANKRUPTCY", "Bankruptcy Search", "Civil", false, 2, null),
            new ServiceConfigDto("HEALTHCARE_SANCTION", "Healthcare Sanctions Check", "Healthcare", false, 2, null)
        ];
    }

    private static IReadOnlyList<TierConfigDto> BuildDefaultTiers()
    {
        return
        [
            new TierConfigDto(1, "Identity & Preliminary Checks", "Initial identity verification and basic searches",
                ["SSN_TRACE", "NATL_CRIM", "SEX_OFFENDER", "GLOBAL_WATCH"], [], true, false),
            new TierConfigDto(2, "Criminal & Driving", "Detailed criminal and motor vehicle checks",
                ["STATE_CRIM", "FED_CRIM"], ["COUNTY_CRIM", "MVR", "CIVIL_COURT", "BANKRUPTCY", "HEALTHCARE_SANCTION"],
                true, true),
            new TierConfigDto(3, "Employment & Education", "Employment and education verifications",
                [], ["TWN_EMP", "MANUAL_EMP", "EDU_VERIFY", "PRO_LICENSE", "CREDIT_CHECK"], false, true),
            new TierConfigDto(4, "Additional Checks", "Drug screening and reference checks",
                [], ["REF_CHECK", "DRUG_5", "DRUG_10"], false, true)
        ];
    }
}