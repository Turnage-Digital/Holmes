using Holmes.Core.Application;
using Holmes.Core.Contracts;
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