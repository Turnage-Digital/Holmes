using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Abstractions.Commands;

public sealed record StartIntakeSessionCommand(
    UlidId IntakeSessionId,
    string ResumeToken,
    DateTimeOffset StartedAt,
    string? DeviceInfo
) : RequestBase<Result>;