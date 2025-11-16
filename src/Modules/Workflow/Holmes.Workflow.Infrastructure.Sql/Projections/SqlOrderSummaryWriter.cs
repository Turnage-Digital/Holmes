using Holmes.Workflow.Application.Projections;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Workflow.Infrastructure.Sql.Projections;

public sealed class SqlOrderSummaryWriter(WorkflowDbContext dbContext)
    : IOrderSummaryWriter
{
    public async Task UpsertAsync(Order order, CancellationToken cancellationToken)
    {
        var record = await dbContext.OrderSummaries
            .FirstOrDefaultAsync(x => x.OrderId == order.Id.ToString(), cancellationToken);

        if (record is null)
        {
            record = new OrderSummaryProjectionDb
            {
                OrderId = order.Id.ToString(),
                CreatedAt = order.CreatedAt,
                SubjectId = order.SubjectId.ToString(),
                CustomerId = order.CustomerId.ToString(),
                PolicySnapshotId = order.PolicySnapshotId
            };
            dbContext.OrderSummaries.Add(record);
        }

        record.SubjectId = order.SubjectId.ToString();
        record.CustomerId = order.CustomerId.ToString();
        record.PolicySnapshotId = order.PolicySnapshotId;
        record.PackageCode = order.PackageCode;
        record.Status = order.Status.ToString();
        record.LastStatusReason = order.LastStatusReason;
        record.LastUpdatedAt = order.LastUpdatedAt;
        record.ReadyForRoutingAt = order.ReadyForRoutingAt;
        record.ClosedAt = order.ClosedAt;
        record.CanceledAt = order.CanceledAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}