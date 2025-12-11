using Holmes.Services.Application.Abstractions;
using Holmes.Services.Domain;

namespace Holmes.Services.Infrastructure.Sql;

public sealed class VendorAdapterFactory : IVendorAdapterFactory
{
    private readonly IReadOnlyDictionary<string, IVendorAdapter> _adapters;

    public VendorAdapterFactory(IEnumerable<IVendorAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(
            a => a.VendorCode,
            a => a,
            StringComparer.OrdinalIgnoreCase);
    }

    public IVendorAdapter? GetAdapter(string vendorCode)
    {
        return _adapters.TryGetValue(vendorCode, out var adapter) ? adapter : null;
    }

    public IVendorAdapter? GetAdapterForCategory(ServiceCategory category)
    {
        return _adapters.Values.FirstOrDefault(a => a.SupportedCategories.Contains(category));
    }

    public IEnumerable<IVendorAdapter> GetAll()
    {
        return _adapters.Values;
    }
}