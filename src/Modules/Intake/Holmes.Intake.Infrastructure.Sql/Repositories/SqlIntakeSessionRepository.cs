using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Intake.Infrastructure.Sql.Repositories;

public class SqlIntakeSessionRepository(IntakeDbContext dbContext)
    : IIntakeSessionRepository
{
    public Task AddAsync(IntakeSession session, CancellationToken cancellationToken)
    {
        var db = IntakeSessionMapper.ToDb(session);
        dbContext.IntakeSessions.Add(db);
        return Task.CompletedTask;
    }

    public async Task<IntakeSession?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var db = await dbContext.IntakeSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IntakeSessionId == id.ToString(), cancellationToken);

        return db is null ? null : IntakeSessionMapper.ToDomain(db);
    }

    public async Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken)
    {
        var db = await dbContext.IntakeSessions
            .FirstOrDefaultAsync(x => x.IntakeSessionId == session.Id.ToString(), cancellationToken);

        if (db is null)
        {
            throw new InvalidOperationException($"Intake session '{session.Id}' not found.");
        }

        IntakeSessionMapper.UpdateDb(db, session);
    }
}