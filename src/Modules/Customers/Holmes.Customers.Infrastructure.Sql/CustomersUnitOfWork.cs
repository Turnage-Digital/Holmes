using Holmes.Core.Infrastructure.Sql;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Customers.Infrastructure.Sql;

public sealed class CustomersUnitOfWork(CustomersDbContext dbContext, IMediator mediator)
    : UnitOfWork<CustomersDbContext>(dbContext, mediator), ICustomersUnitOfWork
{
    private readonly Lazy<ICustomerRepository> _customers = new(() => new SqlCustomerRepository(dbContext));

    public ICustomerRepository Customers => _customers.Value;
}