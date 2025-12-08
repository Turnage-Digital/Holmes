using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Vendors;

namespace Holmes.Services.Infrastructure.Sql.Vendors;

/// <summary>
/// In-memory credential store for development and testing.
/// Production should use Key Vault or similar.
/// </summary>
public sealed class InMemoryVendorCredentialStore : IVendorCredentialStore
{
    private readonly Dictionary<(string CustomerId, string VendorCode), VendorCredential> _credentials = new();

    public Task<VendorCredential?> GetAsync(
        UlidId customerId,
        string vendorCode,
        CancellationToken cancellationToken = default)
    {
        var key = (customerId.ToString(), vendorCode.ToUpperInvariant());

        if (_credentials.TryGetValue(key, out var credential))
        {
            return Task.FromResult<VendorCredential?>(credential);
        }

        // For STUB vendor, return dummy credentials
        if (vendorCode.Equals("STUB", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<VendorCredential?>(new VendorCredential(
                ApiKey: "stub-api-key",
                AccountId: "stub-account",
                SecretKey: null,
                Metadata: null));
        }

        return Task.FromResult<VendorCredential?>(null);
    }

    public void SetCredential(UlidId customerId, string vendorCode, VendorCredential credential)
    {
        var key = (customerId.ToString(), vendorCode.ToUpperInvariant());
        _credentials[key] = credential;
    }
}
