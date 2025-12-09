using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Workflow.Infrastructure.Sql;

public sealed class WorkflowUnitOfWork(
    WorkflowDbContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null)
    : UnitOfWork<WorkflowDbContext>(dbContext, mediator, eventStore, serializer, tenantContext), IWorkflowUnitOfWork
{
    private readonly Lazy<IOrderRepository> _orders = new(() => new SqlOrderRepository(dbContext));

    public IOrderRepository Orders => _orders.Value;
}