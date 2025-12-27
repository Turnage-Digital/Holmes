using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Domain;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default);

    Task<Service?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken = default
    );

    void Add(Service service);

    void Update(Service service);
}