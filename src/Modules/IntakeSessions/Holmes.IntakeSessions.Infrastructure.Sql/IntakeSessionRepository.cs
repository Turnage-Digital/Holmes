using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

public class IntakeSessionRepository(IntakeDbContext dbContext)
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