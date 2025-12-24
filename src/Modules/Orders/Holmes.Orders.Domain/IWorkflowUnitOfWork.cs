using Holmes.Core.Domain;

namespace Holmes.Orders.Domain;

public interface IWorkflowUnitOfWork : IUnitOfWork
{
    IOrderRepository Orders { get; }
}