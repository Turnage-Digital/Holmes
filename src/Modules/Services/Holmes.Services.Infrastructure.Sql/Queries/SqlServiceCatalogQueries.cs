using System.Text.Json;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Domain;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Services.Infrastructure.Sql.Queries;

public sealed class SqlServiceCatalogQueries(ServicesDbContext dbContext) : IServiceCatalogQueries
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
        var services = new List<ServiceConfigDto>
        {
            new("SSN_TRACE", "SSN Trace", nameof(ServiceCategory.Identity), true, 1, null),
            new("NATL_CRIM", "National Criminal Search", nameof(ServiceCategory.Criminal), true, 1, null),
            new("STATE_CRIM", "State Criminal Search", nameof(ServiceCategory.Criminal), true, 2, null),
            new("COUNTY_CRIM", "County Criminal Search", nameof(ServiceCategory.Criminal), true, 2, null),
            new("FED_CRIM", "Federal Criminal Search", nameof(ServiceCategory.Criminal), true, 2, null),
            new("SEX_OFFENDER", "Sex Offender Registry", nameof(ServiceCategory.Criminal), true, 1, null),
            new("GLOBAL_WATCH", "Global Watchlist Search", nameof(ServiceCategory.Criminal), true, 1, null),
            new("MVR", "Motor Vehicle Report", nameof(ServiceCategory.Driving), true, 2, null),
            new("CREDIT_CHECK", "Credit Report", nameof(ServiceCategory.Credit), false, 3, null),
            new("TWN_EMP", "Employment Verification (TWN)", nameof(ServiceCategory.Employment), true, 3, null),
            new("MANUAL_EMP", "Employment Verification (Manual)", nameof(ServiceCategory.Employment), false, 3, null),
            new("EDU_VERIFY", "Education Verification", nameof(ServiceCategory.Education), true, 3, null),
            new("PRO_LICENSE", "Professional License Verification", nameof(ServiceCategory.Employment), false, 3, null),
            new("REF_CHECK", "Reference Check", nameof(ServiceCategory.Reference), false, 4, null),
            new("DRUG_5", "5-Panel Drug Screen", nameof(ServiceCategory.Drug), false, 4, null),
            new("DRUG_10", "10-Panel Drug Screen", nameof(ServiceCategory.Drug), false, 4, null),
            new("CIVIL_COURT", "Civil Court Search", nameof(ServiceCategory.Civil), false, 2, null),
            new("BANKRUPTCY", "Bankruptcy Search", nameof(ServiceCategory.Civil), false, 2, null),
            new("HEALTHCARE_SANCTION", "Healthcare Sanctions Check", nameof(ServiceCategory.Healthcare), false, 2, null)
        };

        var tiers = new List<TierConfigDto>
        {
            new(1, "Identity & Preliminary Checks", "Initial identity verification and basic searches",
                ["SSN_TRACE", "NATL_CRIM", "SEX_OFFENDER", "GLOBAL_WATCH"], [], true, false),
            new(2, "Criminal & Driving", "Detailed criminal and motor vehicle checks",
                ["STATE_CRIM", "FED_CRIM"], ["COUNTY_CRIM", "MVR", "CIVIL_COURT", "BANKRUPTCY", "HEALTHCARE_SANCTION"],
                true, true),
            new(3, "Employment & Education", "Employment and education verifications",
                [], ["TWN_EMP", "MANUAL_EMP", "EDU_VERIFY", "PRO_LICENSE", "CREDIT_CHECK"], false, true),
            new(4, "Additional Checks", "Drug screening and reference checks",
                [], ["REF_CHECK", "DRUG_5", "DRUG_10"], false, true)
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