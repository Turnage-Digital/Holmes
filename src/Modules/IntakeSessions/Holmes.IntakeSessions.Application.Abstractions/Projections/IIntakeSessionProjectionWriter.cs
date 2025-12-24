using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;

namespace Holmes.IntakeSessions.Application.Abstractions.Projections;

public interface IIntakeSessionProjectionWriter
{
    Task CreateAsync(IntakeSessionProjectionModel model, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        UlidId intakeSessionId,
        Func<IntakeSessionProjectionModel, IntakeSessionProjectionModel> updater,
        CancellationToken cancellationToken
    );
}

public sealed record IntakeSessionProjectionModel(
    UlidId IntakeSessionId,
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    string PolicySnapshotSchemaVersion,
    IntakeSessionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastTouchedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? AcceptedAt,
    string? CancellationReason,
    UlidId? SupersededBySessionId
);