using System.Text.Json;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql;
using Holmes.Intake.Infrastructure.Sql.Entities;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql;
using Holmes.Workflow.Infrastructure.Sql.Entities;
using Holmes.Workflow.Infrastructure.Sql.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Holmes.Workflow.Tests;

public sealed class OrderTimelineProjectionRunnerTests : IDisposable
{
    private readonly WorkflowDbContext _workflowDbContext;
    private readonly IntakeDbContext _intakeDbContext;
    private readonly CoreDbContext _coreDbContext;
    private readonly OrderTimelineProjectionRunner _runner;

    public OrderTimelineProjectionRunnerTests()
    {
        var workflowOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var intakeOptions = new DbContextOptionsBuilder<IntakeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var coreOptions = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _workflowDbContext = new WorkflowDbContext(workflowOptions);
        _intakeDbContext = new IntakeDbContext(intakeOptions);
        _coreDbContext = new CoreDbContext(coreOptions);

        var timelineWriter = new SqlOrderTimelineWriter(
            _workflowDbContext,
            NullLogger<SqlOrderTimelineWriter>.Instance);

        _runner = new OrderTimelineProjectionRunner(
            _workflowDbContext,
            _intakeDbContext,
            _coreDbContext,
            timelineWriter,
            NullLogger<OrderTimelineProjectionRunner>.Instance);
    }

    [Fact]
    public async Task RunAsync_RebuildsTimelineFromOrdersAndIntakeSessions()
    {
        var orderId = Ulid.NewUlid().ToString();
        var createdAt = DateTimeOffset.UtcNow;

        _workflowDbContext.Orders.Add(new OrderDb
        {
            OrderId = orderId,
            SubjectId = Ulid.NewUlid().ToString(),
            CustomerId = Ulid.NewUlid().ToString(),
            PolicySnapshotId = "policy-v1",
            Status = OrderStatus.ReadyForRouting.ToString(),
            CreatedAt = createdAt,
            LastUpdatedAt = createdAt.AddMinutes(30),
            InvitedAt = createdAt.AddMinutes(1),
            IntakeStartedAt = createdAt.AddMinutes(2),
            IntakeCompletedAt = createdAt.AddMinutes(10),
            ReadyForRoutingAt = createdAt.AddMinutes(30)
        });

        _intakeDbContext.IntakeSessions.Add(new IntakeSessionDb
        {
            IntakeSessionId = Ulid.NewUlid().ToString(),
            OrderId = orderId,
            SubjectId = Ulid.NewUlid().ToString(),
            CustomerId = Ulid.NewUlid().ToString(),
            Status = IntakeSessionStatus.Submitted.ToString(),
            CreatedAt = createdAt.AddMinutes(1),
            LastTouchedAt = createdAt.AddMinutes(15),
            ExpiresAt = createdAt.AddDays(7),
            ResumeToken = "resume",
            PolicySnapshotJson = JsonSerializer.Serialize(new PolicySnapshotRecord(
                "policy-v1",
                "schema-v1",
                createdAt,
                new Dictionary<string, string>())),
            SubmittedAt = createdAt.AddMinutes(12),
            AcceptedAt = createdAt.AddMinutes(20),
            ConsentArtifactId = Ulid.NewUlid().ToString(),
            ConsentMimeType = "application/pdf",
            ConsentLength = 42,
            ConsentHash = "hash",
            ConsentHashAlgorithm = "SHA256",
            ConsentSchemaVersion = "v1",
            ConsentCapturedAt = createdAt.AddMinutes(11)
        });

        await _workflowDbContext.SaveChangesAsync();
        await _intakeDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(reset: true, CancellationToken.None);

        Assert.True(result.Processed >= 5);
        var events = await _workflowDbContext.OrderTimelineEvents
            .OrderBy(e => e.OccurredAt)
            .ToListAsync();

        Assert.Contains(events, e => e.EventType == "order.status_changed");
        Assert.Contains(events, e => e.EventType.StartsWith("intake."));

        var checkpoint = Assert.Single(_coreDbContext.ProjectionCheckpoints);
        Assert.Equal("workflow.order_timeline", checkpoint.ProjectionName);
    }

    public void Dispose()
    {
        _workflowDbContext.Dispose();
        _intakeDbContext.Dispose();
        _coreDbContext.Dispose();
    }

    private sealed record PolicySnapshotRecord(
        string SnapshotId,
        string SchemaVersion,
        DateTimeOffset CapturedAt,
        IReadOnlyDictionary<string, string> Metadata
    );
}
