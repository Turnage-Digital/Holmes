using Holmes.Orders.Domain;

namespace Holmes.Orders.Application.Abstractions;

public interface IOrderSummaryWriter
{
    Task UpsertAsync(
        Order order,
        CancellationToken cancellationToken
    );
}