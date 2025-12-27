using System.Text.Json;
using Holmes.Services.Application.Abstractions;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Domain;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Services.Infrastructure.Sql;

public sealed class ServiceCatalogQueries(ServicesDbContext dbContext) : IServiceCatalogQueries
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<CustomerServiceCatalogDto> GetByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        var latestSnapshot = await dbContext.ServiceCatalogSnapshots
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSnapshot is null)
        {
            return BuildDefaultCatalog(customerId);
        }

        return ParseCatalogFromSnapshot(customerId, latestSnapshot.ConfigJson, latestSnapshot.CreatedAt);
    }

    public async Task<CatalogConfigDto?> GetConfigByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        var latestSnapshot = await dbContext.ServiceCatalogSnapshots
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSnapshot is null)
        {
            return null;
        }

        return ParseCatalogConfig(latestSnapshot.ConfigJson);
    }

    public async Task<int> GetCurrentVersionAsync(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        var latestSnapshot = await dbContext.ServiceCatalogSnapshots
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Version)
            .Select(s => (int?)s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return latestSnapshot ?? 0;
    }

    private static CustomerServiceCatalogDto BuildDefaultCatalog(string customerId)
    {
        var config = BuildDefaultCatalogConfig();

        return new CustomerServiceCatalogDto(
            customerId,
            config.Services.Select(s => new CatalogServiceItemDto(
                    s.ServiceTypeCode,
                    s.DisplayName,
                    Enum.Parse<ServiceCategory>(s.Category),
                    s.IsEnabled,
                    s.Tier,
                    s.VendorCode
                ))
                .ToList(),
            config.Tiers.Select(t => new TierConfigurationDto(
                    t.Tier,
                    t.Name,
                    t.Description,
                    t.RequiredServices,
                    t.OptionalServices,
                    t.AutoDispatch,
                    t.WaitForPreviousTier
                ))
                .ToList(),
            DateTimeOffset.UtcNow
        );
    }

    private static CatalogConfigDto BuildDefaultCatalogConfig()
    {
        // Service codes must match those defined in ServiceType.cs
        var services = new List<ServiceConfigDto>
        {
            // Identity services (Tier 1)
            new("SSN_TRACE", "SSN Trace", nameof(ServiceCategory.Identity), true, 1, null),
            new("SSN_VERIFY", "SSN Verification", nameof(ServiceCategory.Identity), true, 1, null),
            new("ADDR_VERIFY", "Address Verification", nameof(ServiceCategory.Identity), true, 1, null),
            new("ID_VERIFY", "Identity Verification", nameof(ServiceCategory.Identity), false, 1, null),
            new("DMF", "Death Master File", nameof(ServiceCategory.Identity), true, 1, null),
            new("OFAC", "OFAC Sanctions", nameof(ServiceCategory.Identity), true, 1, null),

            // Criminal services (Tier 2)
            new("FED_CRIM", "Federal Criminal", nameof(ServiceCategory.Criminal), true, 2, null),
            new("STATE_CRIM", "Statewide Criminal", nameof(ServiceCategory.Criminal), true, 2, null),
            new("COUNTY_CRIM", "County Criminal", nameof(ServiceCategory.Criminal), true, 2, null),
            new("MUNI_CRIM", "Municipal Criminal", nameof(ServiceCategory.Criminal), false, 2, null),
            new("SEX_OFF", "Sex Offender Registry", nameof(ServiceCategory.Criminal), true, 2, null),
            new("WATCHLIST", "Global Watchlist", nameof(ServiceCategory.Criminal), true, 2, null),

            // Employment/Education services (Tier 3)
            new("TWN_EMP", "TWN Employment Verification", nameof(ServiceCategory.Employment), true, 3, null),
            new("DIRECT_EMP", "Direct Employment Verification", nameof(ServiceCategory.Employment), false, 3, null),
            new("INCOME_VERIFY", "Income Verification", nameof(ServiceCategory.Employment), false, 3, null),
            new("EDU_VERIFY", "Education Verification", nameof(ServiceCategory.Education), true, 3, null),
            new("PROF_LICENSE", "Professional License", nameof(ServiceCategory.Education), false, 3, null),
            new("CIVIL", "Civil Records Search", nameof(ServiceCategory.Civil), false, 3, null),
            new("PROF_REF", "Professional Reference", nameof(ServiceCategory.Reference), false, 3, null),

            // Expensive services (Tier 4)
            new("MVR", "Motor Vehicle Record", nameof(ServiceCategory.Driving), true, 4, null),
            new("CDL_VERIFY", "CDL Verification", nameof(ServiceCategory.Driving), false, 4, null),
            new("CREDIT", "Credit Check", nameof(ServiceCategory.Credit), false, 4, null),
            new("DRUG_TEST", "Drug Test", nameof(ServiceCategory.Drug), false, 4, null)
        };

        var tiers = new List<TierConfigDto>
        {
            new(1, "Identity & Preliminary Checks", "Initial identity verification and basic searches",
                ["SSN_TRACE", "ADDR_VERIFY", "OFAC", "DMF"], ["SSN_VERIFY", "ID_VERIFY"], true, false),
            new(2, "Criminal Searches", "Criminal background checks",
                ["FED_CRIM", "STATE_CRIM", "WATCHLIST", "SEX_OFF"], ["COUNTY_CRIM", "MUNI_CRIM"],
                true, true),
            new(3, "Employment & Education", "Employment and education verifications",
                [], ["TWN_EMP", "DIRECT_EMP", "EDU_VERIFY", "PROF_LICENSE", "CIVIL", "PROF_REF", "INCOME_VERIFY"],
                false, true),
            new(4, "Additional Checks", "Driving, credit, and drug screening",
                [], ["MVR", "CDL_VERIFY", "CREDIT", "DRUG_TEST"], false, true)
        };

        return new CatalogConfigDto(services, tiers);
    }

    private static CustomerServiceCatalogDto ParseCatalogFromSnapshot(
        string customerId,
        string configJson,
        DateTime createdAt
    )
    {
        var config = ParseCatalogConfig(configJson);

        return new CustomerServiceCatalogDto(
            customerId,
            config.Services.Select(s => new CatalogServiceItemDto(
                    s.ServiceTypeCode,
                    s.DisplayName,
                    Enum.Parse<ServiceCategory>(s.Category),
                    s.IsEnabled,
                    s.Tier,
                    s.VendorCode
                ))
                .ToList(),
            config.Tiers.Select(t => new TierConfigurationDto(
                    t.Tier,
                    t.Name,
                    t.Description,
                    t.RequiredServices,
                    t.OptionalServices,
                    t.AutoDispatch,
                    t.WaitForPreviousTier
                ))
                .ToList(),
            new DateTimeOffset(createdAt, TimeSpan.Zero)
        );
    }

    private static CatalogConfigDto ParseCatalogConfig(string configJson)
    {
        return JsonSerializer.Deserialize<CatalogConfigDto>(configJson, JsonOptions)
               ?? BuildDefaultCatalogConfig();
    }
}