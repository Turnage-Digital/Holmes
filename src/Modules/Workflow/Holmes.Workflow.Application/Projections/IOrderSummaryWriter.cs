using Holmes.Workflow.Domain;

namespace Holmes.Workflow.Application.Projections;

public interface IOrderSummaryWriter
{
    Task UpsertAsync(
        Order order,
        CancellationToken cancellationToken
    );
}