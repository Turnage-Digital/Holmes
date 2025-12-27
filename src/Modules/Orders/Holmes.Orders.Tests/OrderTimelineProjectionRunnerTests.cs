using System.Text.Json;
using Holmes.Core.Infrastructure.Sql;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql;
using Holmes.IntakeSessions.Infrastructure.Sql.Entities;
using Holmes.Orders.Domain;
using Holmes.Orders.Infrastructure.Sql;
using Holmes.Orders.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holmes.Orders.Tests;

public sealed class OrderTimelineProjectionRunnerTests
{
    private CoreDbContext _coreDbContext = null!;
    private IntakeSessionsDbContext _intakeDbContext = null!;
    private OrderTimelineProjectionRunner _runner = null!;
    private OrdersDbContext _workflowDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        var workflowOptions = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var intakeOptions = new DbContextOptionsBuilder<IntakeSessionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var coreOptions = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _workflowDbContext = new OrdersDbContext(workflowOptions);
        _intakeDbContext = new IntakeSessionsDbContext(intakeOptions);
        _coreDbContext = new CoreDbContext(coreOptions);

        var timelineWriter = new OrderTimelineWriter(
            _workflowDbContext,
            NullLogger<OrderTimelineWriter>.Instance);

        var replaySource = new IntakeSessionReplaySource(_intakeDbContext);

        _runner = new OrderTimelineProjectionRunner(
            _workflowDbContext,
            _coreDbContext,
            replaySource,
            timelineWriter,
            NullLogger<OrderTimelineProjectionRunner>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _workflowDbContext.Dispose();
        _intakeDbContext.Dispose();
        _coreDbContext.Dispose();
    }

    [Test]
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
            Status = OrderStatus.ReadyForFulfillment.ToString(),
            CreatedAt = createdAt,
            LastUpdatedAt = createdAt.AddMinutes(30),
            InvitedAt = createdAt.AddMinutes(1),
            IntakeStartedAt = createdAt.AddMinutes(2),
            IntakeCompletedAt = createdAt.AddMinutes(10),
            ReadyForFulfillmentAt = createdAt.AddMinutes(30)
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

        var result = await _runner.RunAsync(true, CancellationToken.None);

        Assert.That(result.Processed, Is.GreaterThanOrEqualTo(5));
        var events = await _workflowDbContext.OrderTimelineEvents
            .OrderBy(e => e.OccurredAt)
            .ToListAsync();

        Assert.Multiple(() =>
        {
            Assert.That(events.Any(e => e.EventType == "order.status_changed"), Is.True);
            Assert.That(events.Any(e => e.EventType.StartsWith("intake.")), Is.True);
        });

        var checkpoint = _coreDbContext.ProjectionCheckpoints.Single();
        Assert.That(checkpoint.ProjectionName, Is.EqualTo("workflow.order_timeline"));
    }

    private sealed record PolicySnapshotRecord(
        string SnapshotId,
        string SchemaVersion,
        DateTimeOffset CapturedAt,
        IReadOnlyDictionary<string, string> Metadata
    );
}