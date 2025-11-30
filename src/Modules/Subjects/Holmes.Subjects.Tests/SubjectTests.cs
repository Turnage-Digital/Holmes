using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Domain.Events;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;

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
    public async Task SqlRepository_AddAsync_PopulatesDirectory()
    {
        await using var context = CreateSubjectsDbContext();
        var repository = new SqlSubjectRepository(context);
        var subject = Subject.Register(UlidId.NewUlid(), "Avery", "Nguyen", null, "avery@example.com",
            DateTimeOffset.UtcNow);
        subject.AddAlias(new SubjectAlias("Ava", "Nguyen", null), DateTimeOffset.UtcNow, UlidId.NewUlid());

        await repository.AddAsync(subject, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var directory = await context.SubjectDirectory.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(directory.SubjectId, Is.EqualTo(subject.Id.ToString()));
            Assert.That(directory.AliasCount, Is.EqualTo(1));
            Assert.That(directory.IsMerged, Is.False);
        });
    }

    [Test]
    public async Task SqlRepository_UpdateAsync_RefreshesDirectory()
    {
        await using var context = CreateSubjectsDbContext();
        var repository = new SqlSubjectRepository(context);
        var subject = Subject.Register(UlidId.NewUlid(), "Avery", "Nguyen", null, null, DateTimeOffset.UtcNow);
        await repository.AddAsync(subject, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        subject.AddAlias(new SubjectAlias("Avery", "Perez", null), DateTimeOffset.UtcNow, UlidId.NewUlid());
        var mergeTarget = UlidId.NewUlid();
        subject.MergeInto(mergeTarget, UlidId.NewUlid(), DateTimeOffset.UtcNow);

        await repository.UpdateAsync(subject, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);

        var directory = await context.SubjectDirectory.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(directory.AliasCount, Is.EqualTo(1));
            Assert.That(directory.IsMerged, Is.True);
        });

        var fetched = await repository.GetByIdAsync(subject.Id, CancellationToken.None);
        Assert.That(fetched, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(fetched!.IsMerged, Is.True);
            Assert.That(fetched.MergedIntoSubjectId, Is.EqualTo(mergeTarget));
        });
    }

    [Test]
    public async Task SqlRepository_GetByIdAsync_RehydratesAliasesAndMergeState()
    {
        await using var context = CreateSubjectsDbContext();
        var repository = new SqlSubjectRepository(context);
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
