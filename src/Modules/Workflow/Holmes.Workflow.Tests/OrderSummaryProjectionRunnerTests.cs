using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql;
using Holmes.Workflow.Infrastructure.Sql.Mappers;
using Holmes.Workflow.Infrastructure.Sql.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holmes.Workflow.Tests;

public sealed class OrderSummaryProjectionRunnerTests
{
    private CoreDbContext _coreDbContext = null!;
    private OrderSummaryProjectionRunner _runner = null!;
    private WorkflowDbContext _workflowDbContext = null!;

    [SetUp]
    public void SetUp()
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

    [TearDown]
    public void TearDown()
    {
        _workflowDbContext.Dispose();
        _coreDbContext.Dispose();
    }

    [Test]
    public async Task Replay_WritesCanceledOrderSummary()
    {
        var order = BuildCanceledOrder(DateTimeOffset.UtcNow);
        _workflowDbContext.Orders.Add(OrderMapper.ToDb(order));
        await _workflowDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(true, CancellationToken.None);

        Assert.That(result.Processed, Is.EqualTo(1));
        var summary = _workflowDbContext.OrderSummaries.Single();
        Assert.Multiple(() =>
        {
            Assert.That(summary.OrderId, Is.EqualTo(order.Id.ToString()));
            Assert.That(summary.Status, Is.EqualTo(OrderStatus.Canceled.ToString()));
            Assert.That(summary.CanceledAt, Is.EqualTo(order.CanceledAt));
            Assert.That(summary.ReadyForRoutingAt, Is.EqualTo(order.ReadyForRoutingAt));
        });

        var checkpoint = _coreDbContext.ProjectionCheckpoints.Single();
        Assert.Multiple(() =>
        {
            Assert.That(checkpoint.ProjectionName, Is.EqualTo("workflow.order_summary"));
            Assert.That(checkpoint.Position, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task Replay_ResumesFromCheckpoint()
    {
        var firstOrder = BuildCanceledOrder(DateTimeOffset.UtcNow);
        _workflowDbContext.Orders.Add(OrderMapper.ToDb(firstOrder));
        await _workflowDbContext.SaveChangesAsync();
        await _runner.RunAsync(true, CancellationToken.None);

        var secondOrder = BuildCanceledOrder(DateTimeOffset.UtcNow.AddHours(1));
        _workflowDbContext.Orders.Add(OrderMapper.ToDb(secondOrder));
        await _workflowDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(false, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Processed, Is.EqualTo(1));
            Assert.That(result.LastEntityId, Is.EqualTo(secondOrder.Id.ToString()));
            Assert.That(_coreDbContext.ProjectionCheckpoints.Single().Position, Is.EqualTo(2));
        });
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