using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Application.EventHandlers;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Domain.Events;
using Holmes.Subjects.Domain.ValueObjects;
using Holmes.Subjects.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holmes.Subjects.Tests;

[TestFixture]
public class SubjectTests
{
    [Test]
    public void Register_Sets_State_And_Emits_Event()
    {
        var id = UlidId.NewUlid();
        var registeredAt = DateTimeOffset.UtcNow;

        var subject = Subject.Register(id, "Avery", "Nguyen", new DateOnly(1993, 7, 4), "avery@example.com",
            registeredAt);

        Assert.Multiple(() =>
        {
            Assert.That(subject.Id, Is.EqualTo(id));
            Assert.That(subject.GivenName, Is.EqualTo("Avery"));
            Assert.That(subject.FamilyName, Is.EqualTo("Nguyen"));
            Assert.That(subject.DateOfBirth, Is.EqualTo(new DateOnly(1993, 7, 4)));
            Assert.That(subject.Email, Is.EqualTo("avery@example.com"));
        });

        var evt = subject.DomainEvents.OfType<SubjectRegistered>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(evt.SubjectId, Is.EqualTo(id));
            Assert.That(evt.RegisteredAt, Is.EqualTo(registeredAt));
        });
    }

    [Test]
    public void Register_Allows_Empty_Names_For_Pending_Intake()
    {
        var id = UlidId.NewUlid();
        var registeredAt = DateTimeOffset.UtcNow;

        var subject = Subject.Register(id, string.Empty, string.Empty, null, "pending@example.com", registeredAt);

        Assert.Multiple(() =>
        {
            Assert.That(subject.GivenName, Is.EqualTo(string.Empty));
            Assert.That(subject.FamilyName, Is.EqualTo(string.Empty));
            Assert.That(subject.Email, Is.EqualTo("pending@example.com"));
        });
    }

    [Test]
    public void AddAlias_Adds_NewAlias_And_Emits_Event()
    {
        var subject = Subject.Register(UlidId.NewUlid(), "Avery", "Nguyen", null, null, DateTimeOffset.UtcNow);
        subject.ClearDomainEvents();
        var alias = new SubjectAlias("Avery", "Perez", new DateOnly(1990, 5, 12));
        var addedBy = UlidId.NewUlid();
        var timestamp = DateTimeOffset.UtcNow;

        subject.AddAlias(alias, timestamp, addedBy);

        Assert.That(subject.Aliases, Has.Count.EqualTo(1));
        var evt = subject.DomainEvents.OfType<SubjectAliasAdded>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(evt.SubjectId, Is.EqualTo(subject.Id));
            Assert.That(evt.AddedBy, Is.EqualTo(addedBy));
            Assert.That(evt.AddedAt, Is.EqualTo(timestamp));
        });
    }

    [Test]
    public void MergeInto_Sets_State_And_Emits_Event()
    {
        var subject = Subject.Register(UlidId.NewUlid(), "Avery", "Nguyen", null, null, DateTimeOffset.UtcNow);
        subject.ClearDomainEvents();
        var target = UlidId.NewUlid();
        var mergedBy = UlidId.NewUlid();
        var mergedAt = DateTimeOffset.UtcNow;

        subject.MergeInto(target, mergedBy, mergedAt);

        Assert.Multiple(() =>
        {
            Assert.That(subject.IsMerged, Is.True);
            Assert.That(subject.MergedIntoSubjectId, Is.EqualTo(target));
            Assert.That(subject.MergedBy, Is.EqualTo(mergedBy));
            Assert.That(subject.MergedAt, Is.EqualTo(mergedAt));
        });

        var evt = subject.DomainEvents.OfType<SubjectMerged>().Single();
        Assert.Multiple(() =>
        {
            Assert.That(evt.SourceSubjectId, Is.EqualTo(subject.Id));
            Assert.That(evt.TargetSubjectId, Is.EqualTo(target));
        });
    }

    [Test]
    public void MergeInto_Prevents_SelfMerge()
    {
        var subject = Subject.Register(UlidId.NewUlid(), "Avery", "Nguyen", null, null, DateTimeOffset.UtcNow);
        subject.ClearDomainEvents();

        Assert.That(
            () => subject.MergeInto(subject.Id, UlidId.NewUlid(), DateTimeOffset.UtcNow),
            Throws.InvalidOperationException);
    }

    [Test]
    public async Task SubjectProjectionHandler_SubjectRegistered_WritesProjection()
    {
        await using var context = CreateSubjectsDbContext();
        var writer = new SubjectProjectionWriter(context, NullLogger<SubjectProjectionWriter>.Instance);
        var handler = new SubjectProjectionHandler(writer);
        var subjectId = UlidId.NewUlid();

        var @event = new SubjectRegistered(subjectId, "Avery", "Nguyen", null, "avery@example.com",
            DateTimeOffset.UtcNow);
        await handler.Handle(@event, CancellationToken.None);

        var projection = await context.SubjectProjections.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(projection.SubjectId, Is.EqualTo(subjectId.ToString()));
            Assert.That(projection.GivenName, Is.EqualTo("Avery"));
            Assert.That(projection.FamilyName, Is.EqualTo("Nguyen"));
            Assert.That(projection.AliasCount, Is.EqualTo(0));
            Assert.That(projection.IsMerged, Is.False);
        });
    }

    [Test]
    public async Task SubjectProjectionHandler_AliasAndMerge_UpdatesProjection()
    {
        await using var context = CreateSubjectsDbContext();
        var writer = new SubjectProjectionWriter(context, NullLogger<SubjectProjectionWriter>.Instance);
        var handler = new SubjectProjectionHandler(writer);
        var subjectId = UlidId.NewUlid();

        // First create the projection
        var registered = new SubjectRegistered(subjectId, "Avery", "Nguyen", null, null, DateTimeOffset.UtcNow);
        await handler.Handle(registered, CancellationToken.None);

        // Add an alias
        var aliasEvent =
            new SubjectAliasAdded(subjectId, "Avery", "Perez", null, UlidId.NewUlid(), DateTimeOffset.UtcNow);
        await handler.Handle(aliasEvent, CancellationToken.None);

        // Merge the subject
        var mergeTarget = UlidId.NewUlid();
        var mergeEvent = new SubjectMerged(subjectId, mergeTarget, UlidId.NewUlid(), DateTimeOffset.UtcNow);
        await handler.Handle(mergeEvent, CancellationToken.None);

        var projection = await context.SubjectProjections.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(projection.AliasCount, Is.EqualTo(1));
            Assert.That(projection.IsMerged, Is.True);
        });
    }

    [Test]
    public async Task SqlRepository_GetByIdAsync_RehydratesAliasesAndMergeState()
    {
        await using var context = CreateSubjectsDbContext();
        var repository = new SubjectRepository(context);
        var subject = Subject.Register(UlidId.NewUlid(), "Avery", "Nguyen", null, null, DateTimeOffset.UtcNow);
        subject.AddAlias(new SubjectAlias("Ava", "Nguyen", null), DateTimeOffset.UtcNow, UlidId.NewUlid());
        await repository.AddAsync(subject, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var fetched = await repository.GetByIdAsync(subject.Id, CancellationToken.None);

        Assert.That(fetched, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(fetched!.Aliases, Has.Count.EqualTo(1));
            Assert.That(fetched.IsMerged, Is.False);
        });
    }

    private static SubjectsDbContext CreateSubjectsDbContext()
    {
        var options = new DbContextOptionsBuilder<SubjectsDbContext>()
            .UseInMemoryDatabase($"subjects-tests-{Guid.NewGuid()}")
            .Options;
        return new SubjectsDbContext(options);
    }
}