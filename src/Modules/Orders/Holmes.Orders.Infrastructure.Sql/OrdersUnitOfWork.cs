using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Infrastructure.Sql;

public sealed class OrdersUnitOfWork(
    OrdersDbContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<OrdersDbContext>(dbContext, mediator, eventStore, serializer, tenantContext), IOrdersUnitOfWork
{
    private readonly Lazy<IOrderRepository> _orders = new(() => new OrderRepository(dbContext));

    public IOrderRepository Orders => _orders.Value;
}
