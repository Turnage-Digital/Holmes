using Holmes.Orders.Domain;

namespace Holmes.Orders.Contracts;

public interface IOrderSummaryWriter
{
    Task UpsertAsync(
        Order order,
        CancellationToken cancellationToken
    );
}