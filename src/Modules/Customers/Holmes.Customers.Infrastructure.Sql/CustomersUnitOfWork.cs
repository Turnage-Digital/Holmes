using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Customers.Infrastructure.Sql;

public sealed class CustomersUnitOfWork(
    CustomersDbContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null)
    : UnitOfWork<CustomersDbContext>(dbContext, mediator, eventStore, serializer, tenantContext), ICustomersUnitOfWork
{
    private readonly Lazy<ICustomerRepository> _customers = new(() => new SqlCustomerRepository(dbContext));

    public ICustomerRepository Customers => _customers.Value;
}