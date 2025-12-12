using Holmes.Subjects.Application.Abstractions.Projections;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Subjects.Infrastructure.Sql.Projections;

public sealed class SqlSubjectProjectionWriter(
    SubjectsDbContext dbContext,
    ILogger<SqlSubjectProjectionWriter> logger
) : ISubjectProjectionWriter
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

        if (record is null)
        {
            logger.LogWarning("Subject projection not found for merge update: {SubjectId}", subjectId);
            return;
        }

        record.IsMerged = isMerged;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementAliasCountAsync(string subjectId, CancellationToken cancellationToken)
    {
        var record = await dbContext.SubjectProjections
            .FirstOrDefaultAsync(x => x.SubjectId == subjectId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Subject projection not found for alias count increment: {SubjectId}", subjectId);
            return;
        }

        record.AliasCount++;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}