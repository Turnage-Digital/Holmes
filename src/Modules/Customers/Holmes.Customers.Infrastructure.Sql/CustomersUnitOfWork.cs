using Holmes.Core.Domain;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Infrastructure.Sql;

public sealed class CustomersUnitOfWork : UnitOfWork<CustomersDbContext>, ICustomersUnitOfWork
{
    public CustomersUnitOfWork(CustomersDbContext dbContext, IMediator mediator)
        : base(dbContext, mediator)
    {
    }

    public void RegisterDomainEvents(IHasDomainEvents aggregate)
    {
        if (aggregate is null)
        {
            return;
        }

        CollectDomainEvents(aggregate);
    }

    public void RegisterDomainEvents(IEnumerable<IHasDomainEvents> aggregates)
    {
        if (aggregates is null)
        {
            return;
        }

        CollectDomainEvents(aggregates);
    }
}