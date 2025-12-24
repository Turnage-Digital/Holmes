using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;

namespace Holmes.IntakeSessions.Application.Abstractions;

public interface IIntakeSessionReplaySource
{
    Task<IReadOnlyCollection<IntakeSessionReplayRecord>> ListSessionsAsync(CancellationToken cancellationToken);
}

public sealed record IntakeSessionReplayRecord(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastTouchedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConsentCapturedAt,
    string? ConsentArtifactId,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? AcceptedAt,
    IntakeSessionStatus Status,
    string? CancellationReason,
    UlidId? SupersededBySessionId
);