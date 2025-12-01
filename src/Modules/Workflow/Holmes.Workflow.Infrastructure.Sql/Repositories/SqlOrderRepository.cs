using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Workflow.Infrastructure.Sql.Repositories;

public sealed class SqlOrderRepository(WorkflowDbContext dbContext) : IOrderRepository
{
    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        dbContext.Orders.Add(OrderEntityMapper.ToEntity(order));
        return Task.CompletedTask;
    }

    public async Task<Order?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var record = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == id.ToString(), cancellationToken);

        return record is null ? null : OrderEntityMapper.Rehydrate(record);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken)
    {
        var record = await dbContext.Orders
            .FirstOrDefaultAsync(x => x.OrderId == order.Id.ToString(), cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException($"Order '{order.Id}' not found.");
        }

        OrderEntityMapper.Apply(order, record);
    }
}