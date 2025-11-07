using Holmes.Core.Domain;

namespace Holmes.Customers.Domain;

public interface ICustomersUnitOfWork : IUnitOfWork
{
    ICustomerRepository Customers { get; }
}
