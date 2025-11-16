using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Projections;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Intake.Infrastructure.Sql.Projections;

public sealed class SqlIntakeSessionProjectionWriter(
    IntakeDbContext dbContext,
    ILogger<SqlIntakeSessionProjectionWriter> logger
)
    : IIntakeSessionProjectionWriter
{
    public async Task CreateAsync(IntakeSessionProjectionModel model, CancellationToken cancellationToken)
    {
        var entity = ToEntity(model);
        dbContext.IntakeSessionProjections.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        UlidId intakeSessionId,
        Func<IntakeSessionProjectionModel, IntakeSessionProjectionModel> updater,
        CancellationToken cancellationToken
    )
    {
        var entity = await dbContext.IntakeSessionProjections
            .FirstOrDefaultAsync(x => x.IntakeSessionId == intakeSessionId.ToString(), cancellationToken);

        if (entity is null)
        {
            logger.LogWarning("Projection missing for IntakeSession {SessionId}", intakeSessionId);
            return false;
        }

        var current = FromEntity(entity);
        var updated = updater(current);
        Apply(updated, entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IntakeSessionProjectionModel FromEntity(IntakeSessionProjectionDb entity)
    {
        return new IntakeSessionProjectionModel(
            UlidId.Parse(entity.IntakeSessionId),
            UlidId.Parse(entity.OrderId),
            UlidId.Parse(entity.SubjectId),
            UlidId.Parse(entity.CustomerId),
            entity.PolicySnapshotId,
            entity.PolicySnapshotSchemaVersion,
            Enum.Parse<IntakeSessionStatus>(entity.Status),
            entity.CreatedAt,
            entity.LastTouchedAt,
            entity.ExpiresAt,
            entity.SubmittedAt,
            entity.AcceptedAt,
            entity.CancellationReason,
            string.IsNullOrWhiteSpace(entity.SupersededBySessionId)
                ? null
                : UlidId.Parse(entity.SupersededBySessionId));
    }

    private static IntakeSessionProjectionDb ToEntity(IntakeSessionProjectionModel model)
    {
        return new IntakeSessionProjectionDb
        {
            IntakeSessionId = model.IntakeSessionId.ToString(),
            OrderId = model.OrderId.ToString(),
            SubjectId = model.SubjectId.ToString(),
            CustomerId = model.CustomerId.ToString(),
            PolicySnapshotId = model.PolicySnapshotId,
            PolicySnapshotSchemaVersion = model.PolicySnapshotSchemaVersion,
            Status = model.Status.ToString(),
            CreatedAt = model.CreatedAt,
            LastTouchedAt = model.LastTouchedAt,
            ExpiresAt = model.ExpiresAt,
            SubmittedAt = model.SubmittedAt,
            AcceptedAt = model.AcceptedAt,
            CancellationReason = model.CancellationReason,
            SupersededBySessionId = model.SupersededBySessionId?.ToString()
        };
    }

    private static void Apply(IntakeSessionProjectionModel model, IntakeSessionProjectionDb entity)
    {
        entity.Status = model.Status.ToString();
        entity.LastTouchedAt = model.LastTouchedAt;
        entity.ExpiresAt = model.ExpiresAt;
        entity.SubmittedAt = model.SubmittedAt;
        entity.AcceptedAt = model.AcceptedAt;
        entity.CancellationReason = model.CancellationReason;
        entity.SupersededBySessionId = model.SupersededBySessionId?.ToString();
    }
}