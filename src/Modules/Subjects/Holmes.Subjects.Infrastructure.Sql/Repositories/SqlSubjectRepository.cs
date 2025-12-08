using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using Holmes.Subjects.Infrastructure.Sql.Mappers;
using Holmes.Subjects.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Subjects.Infrastructure.Sql.Repositories;

public class SqlSubjectRepository(SubjectsDbContext dbContext) : ISubjectRepository
{
    public Task AddAsync(Subject subject, CancellationToken cancellationToken)
    {
        var db = SubjectMapper.ToDb(subject);
        dbContext.Subjects.Add(db);
        UpsertDirectory(subject, db);
        return Task.CompletedTask;
    }

    public async Task<Subject?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var spec = new SubjectWithAllDetailsSpec(id.ToString());

        var db = await dbContext.Subjects
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return db is null ? null : SubjectMapper.ToDomain(db);
    }

    public async Task UpdateAsync(Subject subject, CancellationToken cancellationToken)
    {
        var spec = new SubjectWithAllDetailsSpec(subject.Id.ToString());

        var db = await dbContext.Subjects
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (db is null)
        {
            throw new InvalidOperationException($"Subject '{subject.Id}' not found.");
        }

        SubjectMapper.UpdateDb(db, subject);
        UpsertDirectory(subject, db);
    }

    private void UpsertDirectory(Subject subject, SubjectDb db)
    {
        var record = dbContext.SubjectDirectory.SingleOrDefault(x => x.SubjectId == db.SubjectId);
        if (record is null)
        {
            record = new SubjectDirectoryDb
            {
                SubjectId = db.SubjectId,
                GivenName = db.GivenName,
                FamilyName = db.FamilyName,
                DateOfBirth = db.DateOfBirth,
                Email = db.Email,
                CreatedAt = db.CreatedAt,
                IsMerged = subject.IsMerged,
                AliasCount = db.Aliases.Count
            };
            dbContext.SubjectDirectory.Add(record);
        }
        else
        {
            record.GivenName = db.GivenName;
            record.FamilyName = db.FamilyName;
            record.DateOfBirth = db.DateOfBirth;
            record.Email = db.Email;
            record.IsMerged = subject.IsMerged;
            record.AliasCount = db.Aliases.Count;
        }
    }
}
