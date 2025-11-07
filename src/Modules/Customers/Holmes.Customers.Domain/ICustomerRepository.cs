using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Domain;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(UlidId id, CancellationToken cancellationToken);

    Task AddAsync(Customer customer, CancellationToken cancellationToken);

    Task UpdateAsync(Customer customer, CancellationToken cancellationToken);
}