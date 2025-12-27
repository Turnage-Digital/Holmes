using System.Text.Json;
using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions;
using Holmes.Services.Application.Abstractions.Commands;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed class UpdateCatalogServiceCommandHandler(
    IServiceCatalogQueries catalogQueries,
    IServiceCatalogRepository catalogRepository
) : IRequestHandler<UpdateCatalogServiceCommand, Result>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<Result> Handle(UpdateCatalogServiceCommand request, CancellationToken cancellationToken)
    {
        var existingConfig = await catalogQueries.GetConfigByCustomerIdAsync(
            request.CustomerId, cancellationToken);

        var config = existingConfig ?? BuildDefaultCatalogConfig();

        // Find and update the service
        var services = config.Services.ToList();
        var serviceIndex = services.FindIndex(s =>
            s.ServiceTypeCode.Equals(request.ServiceTypeCode, StringComparison.OrdinalIgnoreCase));

        if (serviceIndex < 0)
        {
            return Result.Fail($"Service type '{request.ServiceTypeCode}' not found in catalog.");
        }

        var existing = services[serviceIndex];
        services[serviceIndex] = existing with
        {
            IsEnabled = request.IsEnabled,
            Tier = request.Tier ?? existing.Tier,
            VendorCode = request.VendorCode ?? existing.VendorCode
        };

        var updatedConfig = new CatalogConfigDto(services, config.Tiers.ToList());

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

    private static CatalogConfigDto BuildDefaultCatalogConfig()
    {
        var services = new List<ServiceConfigDto>
        {
            new("SSN_TRACE", "SSN Trace", "Identity", true, 1, null),
            new("NATL_CRIM", "National Criminal Search", "Criminal", true, 1, null),
            new("STATE_CRIM", "State Criminal Search", "Criminal", true, 2, null),
            new("COUNTY_CRIM", "County Criminal Search", "Criminal", true, 2, null),
            new("FED_CRIM", "Federal Criminal Search", "Criminal", true, 2, null),
            new("SEX_OFFENDER", "Sex Offender Registry", "Criminal", true, 1, null),
            new("GLOBAL_WATCH", "Global Watchlist Search", "Criminal", true, 1, null),
            new("MVR", "Motor Vehicle Report", "Driving", true, 2, null),
            new("CREDIT_CHECK", "Credit Report", "Credit", false, 3, null),
            new("TWN_EMP", "Employment Verification (TWN)", "Employment", true, 3, null),
            new("MANUAL_EMP", "Employment Verification (Manual)", "Employment", false, 3, null),
            new("EDU_VERIFY", "Education Verification", "Education", true, 3, null),
            new("PRO_LICENSE", "Professional License Verification", "Employment", false, 3, null),
            new("REF_CHECK", "Reference Check", "Reference", false, 4, null),
            new("DRUG_5", "5-Panel Drug Screen", "Drug", false, 4, null),
            new("DRUG_10", "10-Panel Drug Screen", "Drug", false, 4, null),
            new("CIVIL_COURT", "Civil Court Search", "Civil", false, 2, null),
            new("BANKRUPTCY", "Bankruptcy Search", "Civil", false, 2, null),
            new("HEALTHCARE_SANCTION", "Healthcare Sanctions Check", "Healthcare", false, 2, null)
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
}