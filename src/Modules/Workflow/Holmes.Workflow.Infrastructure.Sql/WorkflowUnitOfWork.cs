using Holmes.Core.Infrastructure.Sql;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Workflow.Infrastructure.Sql;

public sealed class WorkflowUnitOfWork(WorkflowDbContext dbContext, IMediator mediator)
    : UnitOfWork<WorkflowDbContext>(dbContext, mediator), IWorkflowUnitOfWork
{
    private readonly Lazy<IOrderRepository> _orders = new(() => new SqlOrderRepository(dbContext));

    public IOrderRepository Orders => _orders.Value;
}