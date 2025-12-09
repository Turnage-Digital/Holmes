using Holmes.Subjects.Application.Abstractions.Projections;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Subjects.Infrastructure.Sql.Projections;

public sealed class SqlSubjectProjectionWriter(SubjectsDbContext dbContext) : ISubjectProjectionWriter
{
    public async Task UpsertAsync(SubjectProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.SubjectProjections
            .FirstOrDefaultAsync(x => x.SubjectId == model.SubjectId, cancellationToken);

        if (record is null)
        {
            record = new SubjectProjectionDb
            {
                SubjectId = model.SubjectId
            };
            dbContext.SubjectProjections.Add(record);
        }

        record.GivenName = model.GivenName;
        record.FamilyName = model.FamilyName;
        record.DateOfBirth = model.DateOfBirth;
        record.Email = model.Email;
        record.CreatedAt = model.CreatedAt;
        record.IsMerged = model.IsMerged;
        record.AliasCount = model.AliasCount;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateIsMergedAsync(string subjectId, bool isMerged, CancellationToken cancellationToken)
    {
        var record = await dbContext.SubjectProjections
            .FirstOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (record is not null)
        {
            record.IsMerged = isMerged;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task IncrementAliasCountAsync(string subjectId, CancellationToken cancellationToken)
    {
        var record = await dbContext.SubjectProjections
            .FirstOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (record is not null)
        {
            record.AliasCount++;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
