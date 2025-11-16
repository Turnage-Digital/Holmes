using Holmes.Core.Domain;

namespace Holmes.Workflow.Domain;

public interface IWorkflowUnitOfWork : IUnitOfWork
{
    IOrderRepository Orders { get; }
}