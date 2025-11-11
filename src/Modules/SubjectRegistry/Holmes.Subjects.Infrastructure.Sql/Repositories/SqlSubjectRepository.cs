using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Subjects.Infrastructure.Sql.Repositories;

public class SqlSubjectRepository : ISubjectRepository
{
    private readonly SubjectsDbContext _dbContext;

    public SqlSubjectRepository(SubjectsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Subject subject, CancellationToken cancellationToken)
    {
        var entity = ToDb(subject);
        _dbContext.Subjects.Add(entity);
        UpsertDirectory(subject, entity);
        return Task.CompletedTask;
    }

    public async Task<Subject?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var subjectId = id.ToString();
        var entity = await _dbContext.Subjects
            .Include(s => s.Aliases)
            .FirstOrDefaultAsync(s => s.SubjectId == subjectId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return Rehydrate(entity);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Subjects
            .Include(s => s.Aliases)
            .FirstOrDefaultAsync(s => s.SubjectId == subject.Id.ToString(), cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Subject '{subject.Id}' not found.");
        }

        ApplyState(subject, entity);
        UpsertDirectory(subject, entity);
    }

    private static Subject Rehydrate(SubjectDb entity)
    {
        var aliases = entity.Aliases
            .Select(a => new SubjectAlias(a.GivenName, a.FamilyName, a.DateOfBirth));

        return Subject.Rehydrate(
            UlidId.Parse(entity.SubjectId),
            entity.GivenName,
            entity.FamilyName,
            entity.DateOfBirth,
            entity.Email,
            entity.CreatedAt,
            aliases,
            entity.MergedIntoSubjectId is null ? null : UlidId.Parse(entity.MergedIntoSubjectId),
            entity.MergedBy,
            entity.MergedAt);
    }

    private static void ApplyState(Subject subject, SubjectDb entity)
    {
        entity.GivenName = subject.GivenName;
        entity.FamilyName = subject.FamilyName;
        entity.DateOfBirth = subject.DateOfBirth;
        entity.Email = subject.Email;
        entity.MergedIntoSubjectId = subject.MergedIntoSubjectId?.ToString();
        entity.MergedBy = subject.MergedBy;
        entity.MergedAt = subject.MergedAt;

        SyncAliases(subject, entity);
    }

    private static void SyncAliases(Subject subject, SubjectDb entity)
    {
        var desired = subject.Aliases
            .ToDictionary(a => (a.GivenName, a.FamilyName, a.DateOfBirth), a => a);

        var existing = entity.Aliases
            .ToDictionary(a => (a.GivenName, a.FamilyName, a.DateOfBirth), a => a);

        foreach (var key in existing.Keys.Except(desired.Keys).ToList())
        {
            var alias = existing[key];
            entity.Aliases.Remove(alias);
        }

        foreach (var desiredAlias in desired)
        {
            if (!existing.TryGetValue(desiredAlias.Key, out var alias))
            {
                entity.Aliases.Add(new SubjectAliasDb
                {
                    SubjectId = entity.SubjectId,
                    GivenName = desiredAlias.Value.GivenName,
                    FamilyName = desiredAlias.Value.FamilyName,
                    DateOfBirth = desiredAlias.Value.DateOfBirth
                });
            }
        }
    }

    private static SubjectDb ToDb(Subject subject)
    {
        var entity = new SubjectDb
        {
            SubjectId = subject.Id.ToString(),
            GivenName = subject.GivenName,
            FamilyName = subject.FamilyName,
            DateOfBirth = subject.DateOfBirth,
            Email = subject.Email,
            CreatedAt = subject.CreatedAt,
            MergedIntoSubjectId = subject.MergedIntoSubjectId?.ToString(),
            MergedBy = subject.MergedBy,
            MergedAt = subject.MergedAt
        };

        foreach (var alias in subject.Aliases)
        {
            entity.Aliases.Add(new SubjectAliasDb
            {
                SubjectId = entity.SubjectId,
                GivenName = alias.GivenName,
                FamilyName = alias.FamilyName,
                DateOfBirth = alias.DateOfBirth
            });
        }

        return entity;
    }

    private void UpsertDirectory(Subject subject, SubjectDb entity)
    {
        var record = _dbContext.SubjectDirectory.SingleOrDefault(x => x.SubjectId == entity.SubjectId);
        if (record is null)
        {
            record = new SubjectDirectoryDb
            {
                SubjectId = entity.SubjectId,
                GivenName = entity.GivenName,
                FamilyName = entity.FamilyName,
                DateOfBirth = entity.DateOfBirth,
                Email = entity.Email,
                CreatedAt = entity.CreatedAt,
                IsMerged = subject.IsMerged,
                AliasCount = entity.Aliases.Count
            };
            _dbContext.SubjectDirectory.Add(record);
        }
        else
        {
            record.GivenName = entity.GivenName;
            record.FamilyName = entity.FamilyName;
            record.DateOfBirth = entity.DateOfBirth;
            record.Email = entity.Email;
            record.IsMerged = subject.IsMerged;
            record.AliasCount = entity.Aliases.Count;
        }
    }
}