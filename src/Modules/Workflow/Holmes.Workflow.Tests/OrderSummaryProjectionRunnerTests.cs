using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql;
using Holmes.Workflow.Infrastructure.Sql.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Holmes.Workflow.Tests;

public sealed class OrderSummaryProjectionRunnerTests : IDisposable
{
    private readonly CoreDbContext _coreDbContext;
    private readonly OrderSummaryProjectionRunner _runner;
    private readonly WorkflowDbContext _workflowDbContext;

    public OrderSummaryProjectionRunnerTests()
    {
        var workflowOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var coreOptions = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _workflowDbContext = new WorkflowDbContext(workflowOptions);
        _coreDbContext = new CoreDbContext(coreOptions);

        var writer = new SqlOrderSummaryWriter(_workflowDbContext);
        _runner = new OrderSummaryProjectionRunner(
            _workflowDbContext,
            _coreDbContext,
            writer,
            NullLogger<OrderSummaryProjectionRunner>.Instance);
    }

    public void Dispose()
    {
        _workflowDbContext.Dispose();
        _coreDbContext.Dispose();
    }

    [Fact]
    public async Task Replay_WritesCanceledOrderSummary()
    {
        var order = BuildCanceledOrder(DateTimeOffset.UtcNow);
        _workflowDbContext.Orders.Add(OrderEntityMapper.ToEntity(order));
        await _workflowDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(true, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        var summary = Assert.Single(_workflowDbContext.OrderSummaries);
        Assert.Equal(order.Id.ToString(), summary.OrderId);
        Assert.Equal(OrderStatus.Canceled.ToString(), summary.Status);
        Assert.Equal(order.CanceledAt, summary.CanceledAt);
        Assert.Equal(order.ReadyForRoutingAt, summary.ReadyForRoutingAt);

        var checkpoint = Assert.Single(_coreDbContext.ProjectionCheckpoints);
        Assert.Equal("workflow.order_summary", checkpoint.ProjectionName);
        Assert.Equal(1, checkpoint.Position);
    }

    [Fact]
    public async Task Replay_ResumesFromCheckpoint()
    {
        var firstOrder = BuildCanceledOrder(DateTimeOffset.UtcNow);
        _workflowDbContext.Orders.Add(OrderEntityMapper.ToEntity(firstOrder));
        await _workflowDbContext.SaveChangesAsync();
        await _runner.RunAsync(true, CancellationToken.None);

        var secondOrder = BuildCanceledOrder(DateTimeOffset.UtcNow.AddHours(1));
        _workflowDbContext.Orders.Add(OrderEntityMapper.ToEntity(secondOrder));
        await _workflowDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(false, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(secondOrder.Id.ToString(), result.LastEntityId);
        Assert.Equal(2, _coreDbContext.ProjectionCheckpoints.Single().Position);
    }

    private static Order BuildCanceledOrder(DateTimeOffset baseTime)
    {
        var order = Order.Create(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            "policy-v1",
            baseTime,
            "pkg");

        var sessionId = UlidId.NewUlid();
        order.RecordInvite(sessionId, baseTime.AddMinutes(1));
        order.MarkIntakeInProgress(sessionId, baseTime.AddMinutes(2));
        order.MarkIntakeSubmitted(sessionId, baseTime.AddMinutes(3));
        order.MarkReadyForRouting(baseTime.AddMinutes(4));
        order.Cancel("customer canceled", baseTime.AddMinutes(5));
        return order;
    }
}