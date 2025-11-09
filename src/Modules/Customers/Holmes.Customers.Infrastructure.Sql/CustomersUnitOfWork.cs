using Holmes.Core.Infrastructure.Sql;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Customers.Infrastructure.Sql;

public sealed class CustomersUnitOfWork : UnitOfWork<CustomersDbContext>, ICustomersUnitOfWork
{
    private readonly Lazy<ICustomerRepository> _customers;

    public CustomersUnitOfWork(CustomersDbContext dbContext, IMediator mediator)
        : base(dbContext, mediator)
    {
        _customers = new Lazy<ICustomerRepository>(() => new SqlCustomerRepository(dbContext));
    }

    public ICustomerRepository Customers => _customers.Value;
}
