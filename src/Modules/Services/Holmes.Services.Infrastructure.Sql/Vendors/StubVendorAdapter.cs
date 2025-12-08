using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Vendors;
using Holmes.Services.Domain;

namespace Holmes.Services.Infrastructure.Sql.Vendors;

/// <summary>
///     Stub vendor adapter for development and testing.
///     Returns fixture data with simulated delays.
/// </summary>
public sealed class StubVendorAdapter : IVendorAdapter
{
    public string VendorCode => "STUB";

    public IEnumerable<ServiceCategory> SupportedCategories =>
        Enum.GetValues<ServiceCategory>();

    public async Task<DispatchResult> DispatchAsync(
        ServiceRequest request,
        CancellationToken cancellationToken = default
    )
    {
        // Simulate network delay
        await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

        var referenceId = $"STUB-{Guid.NewGuid():N}";

        return new DispatchResult(
            true,
            referenceId,
            null,
            TimeSpan.FromSeconds(5));
    }

    public Task<ServiceResult> ParseCallbackAsync(
        string callbackPayload,
        CancellationToken cancellationToken = default
    )
    {
        // Return fixture data based on service type embedded in payload
        var resultId = UlidId.NewUlid();
        var now = DateTimeOffset.UtcNow;

        // Default to clear result
        var result = ServiceResult.Clear(resultId, null, now);

        // If payload contains "HIT", return a hit result
        if (callbackPayload.Contains("HIT", StringComparison.OrdinalIgnoreCase))
        {
            var records = new List<NormalizedRecord>
            {
                new CriminalRecord
                {
                    Id = UlidId.NewUlid(),
                    SourceJurisdiction = "US-TX",
                    RecordDate = now,
                    CaseNumber = "2023-CR-12345",
                    Court = "Harris County District Court",
                    ChargeDescription = "Theft of Property",
                    ChargeCategory = "Property Crime",
                    Severity = ChargeSeverity.Misdemeanor,
                    Disposition = "Convicted",
                    DispositionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6)),
                    OffenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1))
                }
            };

            result = ServiceResult.Hit(resultId, records, null, null, now);
        }

        return Task.FromResult(result);
    }

    public async Task<ServiceStatusResult> GetStatusAsync(
        string vendorReferenceId,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken);

        // Stub always returns complete
        return new ServiceStatusResult(
            ServiceResultStatus.Clear,
            true,
            "Completed");
    }
}