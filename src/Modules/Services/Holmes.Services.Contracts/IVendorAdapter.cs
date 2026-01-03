using Holmes.Services.Domain;

namespace Holmes.Services.Contracts;

/// <summary>
///     Anti-corruption layer interface for vendor integrations.
///     Each vendor implements this interface to translate their protocol.
/// </summary>
public interface IVendorAdapter
{
    string VendorCode { get; }

    IEnumerable<ServiceCategory> SupportedCategories { get; }

    Task<DispatchResult> DispatchAsync(
        Service service,
        CancellationToken cancellationToken = default
    );

    Task<ServiceResult> ParseCallbackAsync(
        string callbackPayload,
        CancellationToken cancellationToken = default
    );

    Task<ServiceStatusResult> GetStatusAsync(
        string vendorReferenceId,
        CancellationToken cancellationToken = default
    );
}

public sealed record DispatchResult(
    bool Success,
    string? VendorReferenceId,
    string? ErrorMessage,
    TimeSpan? EstimatedTurnaround
);

public sealed record ServiceStatusResult(
    ServiceResultStatus Status,
    bool IsComplete,
    string? StatusMessage
);