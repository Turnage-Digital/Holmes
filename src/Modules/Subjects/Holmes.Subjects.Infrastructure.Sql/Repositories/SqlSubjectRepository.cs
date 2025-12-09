using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Mappers;
using Holmes.Subjects.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Subjects.Infrastructure.Sql.Repositories;

/// <summary>
/// Write-focused repository for Subject aggregate.
/// Query methods are in SqlSubjectQueries (CQRS pattern).
/// Projections are updated via event handlers (SubjectProjectionHandler).
/// </summary>
public class SqlSubjectRepository(SubjectsDbContext dbContext) : ISubjectRepository
{
    public Task AddAsync(Subject subject, CancellationToken cancellationToken)
    {
        var db = SubjectMapper.ToDb(subject);
        dbContext.Subjects.Add(db);
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
    }
}