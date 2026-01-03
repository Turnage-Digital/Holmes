using Holmes.Services.Domain;

namespace Holmes.Services.Contracts;

/// <summary>
///     Factory for resolving vendor adapters by code.
/// </summary>
public interface IVendorAdapterFactory
{
    IVendorAdapter? GetAdapter(string vendorCode);

    IVendorAdapter? GetAdapterForCategory(ServiceCategory category);
}