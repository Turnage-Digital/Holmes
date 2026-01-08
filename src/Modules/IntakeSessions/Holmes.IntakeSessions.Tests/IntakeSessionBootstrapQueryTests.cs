using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql;
using Holmes.IntakeSessions.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.IntakeSessions.Tests;

public class IntakeSessionBootstrapQueryTests
{
    [Test]
    public async Task Bootstrap_Includes_Disclosure_And_Authorization_Mode()
    {
        var options = new DbContextOptionsBuilder<IntakeSessionsDbContext>()
            .UseInMemoryDatabase($"intake-bootstrap-{Guid.NewGuid()}")
            .Options;

        var sessionId = UlidId.NewUlid().ToString();
        var resumeToken = "resume-token-1";
        var metadata = new Dictionary<string, string>
        {
            { IntakeMetadataKeys.DisclosureId, "disclosure-1" },
            { IntakeMetadataKeys.DisclosureVersion, "disclosure-v1" },
            { IntakeMetadataKeys.DisclosureHash, "DISCLOSUREHASH" },
            { IntakeMetadataKeys.DisclosureFormat, "text" },
            { IntakeMetadataKeys.DisclosureContent, "Disclosure body." },
            { IntakeMetadataKeys.AuthorizationId, "authorization-1" },
            { IntakeMetadataKeys.AuthorizationVersion, "authorization-v1" },
            { IntakeMetadataKeys.AuthorizationHash, "AUTHHASH" },
            { IntakeMetadataKeys.AuthorizationFormat, "text" },
            { IntakeMetadataKeys.AuthorizationContent, "Authorization body." },
            { IntakeMetadataKeys.AuthorizationMode, AuthorizationModes.OneTime }
        };
        var policySnapshotJson = JsonSerializer.Serialize(new
        {
            snapshotId = "policy-default",
            schemaVersion = "v1",
            capturedAt = DateTimeOffset.UtcNow,
            metadata
        });

        await using (var dbContext = new IntakeSessionsDbContext(options))
        {
            dbContext.IntakeSessions.Add(new IntakeSessionDb
            {
                IntakeSessionId = sessionId,
                OrderId = UlidId.NewUlid().ToString(),
                SubjectId = UlidId.NewUlid().ToString(),
                CustomerId = UlidId.NewUlid().ToString(),
                Status = IntakeSessionStatus.InProgress.ToString(),
                CreatedAt = DateTimeOffset.UtcNow,
                LastTouchedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(3),
                ResumeToken = resumeToken,
                PolicySnapshotJson = policySnapshotJson
            });
            await dbContext.SaveChangesAsync();
        }

        await using var readContext = new IntakeSessionsDbContext(options);
        var queries = new IntakeSessionQueries(readContext);

        var bootstrap = await queries.GetBootstrapAsync(UlidId.Parse(sessionId), resumeToken, CancellationToken.None);

        Assert.That(bootstrap, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(bootstrap!.Disclosure, Is.Not.Null);
            Assert.That(bootstrap.Disclosure!.DisclosureId, Is.EqualTo("disclosure-1"));
            Assert.That(bootstrap.Disclosure.DisclosureHash, Is.EqualTo("DISCLOSUREHASH"));
            Assert.That(bootstrap.AuthorizationCopy, Is.Not.Null);
            Assert.That(bootstrap.AuthorizationCopy!.AuthorizationHash, Is.EqualTo("AUTHHASH"));
            Assert.That(bootstrap.AuthorizationMode, Is.EqualTo("one_time"));
        });
    }
}
