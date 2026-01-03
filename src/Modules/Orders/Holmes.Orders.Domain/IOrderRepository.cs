using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Domain;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(UlidId id, CancellationToken cancellationToken);
    Task AddAsync(Order order, CancellationToken cancellationToken);
    Task UpdateAsync(Order order, CancellationToken cancellationToken);
}