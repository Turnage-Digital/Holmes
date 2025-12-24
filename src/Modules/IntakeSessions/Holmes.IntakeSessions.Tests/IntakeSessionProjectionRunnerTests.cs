using System.Text.Json;
using Holmes.Core.Infrastructure.Sql;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql;
using Holmes.IntakeSessions.Infrastructure.Sql.Entities;
using Holmes.IntakeSessions.Infrastructure.Sql.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holmes.IntakeSessions.Tests;

public sealed class IntakeSessionProjectionRunnerTests
{
    private CoreDbContext _coreDbContext = null!;
    private IntakeDbContext _intakeDbContext = null!;
    private IntakeSessionProjectionRunner _runner = null!;

    [SetUp]
    public void SetUp()
    {
        var intakeOptions = new DbContextOptionsBuilder<IntakeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var coreOptions = new DbContextOptionsBuilder<CoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _intakeDbContext = new IntakeDbContext(intakeOptions);
        _coreDbContext = new CoreDbContext(coreOptions);

        var writer = new SqlIntakeSessionProjectionWriter(
            _intakeDbContext,
            NullLogger<SqlIntakeSessionProjectionWriter>.Instance);

        _runner = new IntakeSessionProjectionRunner(
            _intakeDbContext,
            _coreDbContext,
            writer,
            NullLogger<IntakeSessionProjectionRunner>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _intakeDbContext.Dispose();
        _coreDbContext.Dispose();
    }

    [Test]
    public async Task RunAsync_RebuildsProjectionFromCanonicalTable()
    {
        var session = CreateSession();
        _intakeDbContext.IntakeSessions.Add(session);
        await _intakeDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(true, CancellationToken.None);

        Assert.That(result.Processed, Is.EqualTo(1));
        var projection = _intakeDbContext.IntakeSessionProjections.Single();
        Assert.Multiple(() =>
        {
            Assert.That(projection.IntakeSessionId, Is.EqualTo(session.IntakeSessionId));
            Assert.That(projection.Status, Is.EqualTo(IntakeSessionStatus.Invited.ToString()));
        });

        var checkpoint = _coreDbContext.ProjectionCheckpoints.Single();
        Assert.That(checkpoint.ProjectionName, Is.EqualTo("intake.sessions"));
    }

    [Test]
    public async Task RunAsync_UpdatesExistingProjectionRows()
    {
        var session = CreateSession();
        _intakeDbContext.IntakeSessions.Add(session);
        await _intakeDbContext.SaveChangesAsync();

        await _runner.RunAsync(true, CancellationToken.None);

        var submittedAt = session.CreatedAt.AddMinutes(10);
        var entity = await _intakeDbContext.IntakeSessions.SingleAsync();
        entity.Status = IntakeSessionStatus.Submitted.ToString();
        entity.SubmittedAt = submittedAt;
        entity.LastTouchedAt = submittedAt;
        await _intakeDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(false, CancellationToken.None);

        Assert.That(result.Processed, Is.EqualTo(1));
        var projection = await _intakeDbContext.IntakeSessionProjections.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(projection.Status, Is.EqualTo(IntakeSessionStatus.Submitted.ToString()));
            Assert.That(projection.SubmittedAt, Is.EqualTo(submittedAt));
        });
    }

    private static IntakeSessionDb CreateSession()
    {
        var sessionId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        var subjectId = Ulid.NewUlid().ToString();
        var customerId = Ulid.NewUlid().ToString();
        var createdAt = DateTimeOffset.UtcNow;

        return new IntakeSessionDb
        {
            IntakeSessionId = sessionId,
            OrderId = orderId,
            SubjectId = subjectId,
            CustomerId = customerId,
            Status = IntakeSessionStatus.Invited.ToString(),
            CreatedAt = createdAt,
            LastTouchedAt = createdAt,
            ExpiresAt = createdAt.AddDays(7),
            ResumeToken = Ulid.NewUlid().ToString(),
            PolicySnapshotJson = JsonSerializer.Serialize(new PolicySnapshotRecord(
                "policy-v1",
                "schema-v1",
                createdAt,
                new Dictionary<string, string>()))
        };
    }

    private sealed record PolicySnapshotRecord(
        string SnapshotId,
        string SchemaVersion,
        DateTimeOffset CapturedAt,
        IReadOnlyDictionary<string, string> Metadata
    );
}