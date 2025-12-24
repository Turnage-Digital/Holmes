using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Abstractions;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

public sealed class IntakeSessionReplaySource(IntakeDbContext dbContext) : IIntakeSessionReplaySource
{
    public async Task<IReadOnlyCollection<IntakeSessionReplayRecord>> ListSessionsAsync(
        CancellationToken cancellationToken
    )
    {
        var sessions = await dbContext.IntakeSessions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return sessions
            .Select(Map)
            .ToList();
    }

    private static IntakeSessionReplayRecord Map(IntakeSessionDb entity)
    {
        UlidId? superseded = string.IsNullOrWhiteSpace(entity.SupersededBySessionId)
            ? null
            : UlidId.Parse(entity.SupersededBySessionId);

        var artifactId = string.IsNullOrWhiteSpace(entity.ConsentArtifactId)
            ? null
            : entity.ConsentArtifactId;

        return new IntakeSessionReplayRecord(
            UlidId.Parse(entity.IntakeSessionId),
            UlidId.Parse(entity.OrderId),
            entity.CreatedAt,
            entity.LastTouchedAt,
            entity.ExpiresAt,
            entity.ConsentCapturedAt,
            artifactId,
            entity.SubmittedAt,
            entity.AcceptedAt,
            Enum.Parse<IntakeSessionStatus>(entity.Status),
            entity.CancellationReason,
            superseded);
    }
}