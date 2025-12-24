using Holmes.Orders.Domain;

namespace Holmes.Orders.Application.Abstractions.Projections;

public interface IOrderSummaryWriter
{
    Task UpsertAsync(
        Order order,
        CancellationToken cancellationToken
    );
}