using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Entities;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql;
using Holmes.Intake.Infrastructure.Sql.Entities;
using Holmes.Intake.Infrastructure.Sql.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Holmes.Intake.Tests;

public sealed class IntakeSessionProjectionRunnerTests : IDisposable
{
    private readonly IntakeDbContext _intakeDbContext;
    private readonly CoreDbContext _coreDbContext;
    private readonly IntakeSessionProjectionRunner _runner;

    public IntakeSessionProjectionRunnerTests()
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

    [Fact]
    public async Task RunAsync_RebuildsProjectionFromCanonicalTable()
    {
        var session = CreateSession();
        _intakeDbContext.IntakeSessions.Add(session);
        await _intakeDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(reset: true, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        var projection = Assert.Single(_intakeDbContext.IntakeSessionProjections);
        Assert.Equal(session.IntakeSessionId, projection.IntakeSessionId);
        Assert.Equal(IntakeSessionStatus.Invited.ToString(), projection.Status);

        var checkpoint = Assert.Single(_coreDbContext.ProjectionCheckpoints);
        Assert.Equal("intake.sessions", checkpoint.ProjectionName);
    }

    [Fact]
    public async Task RunAsync_UpdatesExistingProjectionRows()
    {
        var session = CreateSession();
        _intakeDbContext.IntakeSessions.Add(session);
        await _intakeDbContext.SaveChangesAsync();

        await _runner.RunAsync(reset: true, CancellationToken.None);

        var submittedAt = session.CreatedAt.AddMinutes(10);
        var entity = await _intakeDbContext.IntakeSessions.SingleAsync();
        entity.Status = IntakeSessionStatus.Submitted.ToString();
        entity.SubmittedAt = submittedAt;
        entity.LastTouchedAt = submittedAt;
        await _intakeDbContext.SaveChangesAsync();

        var result = await _runner.RunAsync(reset: false, CancellationToken.None);

        Assert.Equal(1, result.Processed);
        var projection = await _intakeDbContext.IntakeSessionProjections.SingleAsync();
        Assert.Equal(IntakeSessionStatus.Submitted.ToString(), projection.Status);
        Assert.Equal(submittedAt, projection.SubmittedAt);
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

    public void Dispose()
    {
        _intakeDbContext.Dispose();
        _coreDbContext.Dispose();
    }
}
