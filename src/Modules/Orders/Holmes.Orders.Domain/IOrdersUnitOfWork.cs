using Holmes.Core.Domain;

namespace Holmes.Orders.Domain;

public interface IOrdersUnitOfWork : IUnitOfWork
{
    IOrderRepository Orders { get; }
}
