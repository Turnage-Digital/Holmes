using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Application.Vendors;

/// <summary>
/// Infrastructure port for retrieving vendor credentials.
/// Implementations may use Key Vault, Secrets Manager, or encrypted DB.
/// </summary>
public interface IVendorCredentialStore
{
    Task<VendorCredential?> GetAsync(
        UlidId customerId,
        string vendorCode,
        CancellationToken cancellationToken = default);
}

public sealed record VendorCredential(
    string ApiKey,
    string? AccountId,
    string? SecretKey,
    IReadOnlyDictionary<string, string>? Metadata);
