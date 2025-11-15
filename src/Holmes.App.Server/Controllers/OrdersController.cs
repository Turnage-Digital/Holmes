using Holmes.Workflow.Infrastructure.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController(WorkflowDbContext dbContext) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IReadOnlyCollection<OrderSummaryResponse>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var summaries = await dbContext.OrderSummaries
            .AsNoTracking()
            .OrderByDescending(o => o.LastUpdatedAt)
            .ToListAsync(cancellationToken);

        return summaries.Select(o => new OrderSummaryResponse(
                o.OrderId,
                o.SubjectId,
                o.CustomerId,
                o.PolicySnapshotId,
                o.PackageCode,
                o.Status,
                o.LastStatusReason,
                o.LastUpdatedAt,
                o.ReadyForRoutingAt,
                o.ClosedAt,
                o.CanceledAt))
            .ToArray();
    }
}

public sealed record OrderSummaryResponse(
    string OrderId,
    string SubjectId,
    string CustomerId,
    string PolicySnapshotId,
    string? PackageCode,
    string Status,
    string? LastStatusReason,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? ReadyForRoutingAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? CanceledAt
);