using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record SaveIntakeProgressCommand(
    UlidId IntakeSessionId,
    string ResumeToken,
    string SchemaVersion,
    string PayloadHash,
    string PayloadCipherText,
    DateTimeOffset UpdatedAt
) : RequestBase<Result>;