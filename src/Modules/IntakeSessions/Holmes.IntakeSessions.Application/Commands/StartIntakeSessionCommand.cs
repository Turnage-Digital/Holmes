using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record StartIntakeSessionCommand(
    UlidId IntakeSessionId,
    string ResumeToken,
    DateTimeOffset StartedAt,
    string? DeviceInfo
) : RequestBase<Result>;