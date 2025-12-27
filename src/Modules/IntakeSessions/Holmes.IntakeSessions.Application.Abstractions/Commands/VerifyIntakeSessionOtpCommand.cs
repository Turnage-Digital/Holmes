using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Abstractions.Commands;

public sealed record VerifyIntakeSessionOtpCommand(
    UlidId IntakeSessionId,
    string Code
) : RequestBase<Result>;
