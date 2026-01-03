using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record SubmitIntakeCommand(
    UlidId IntakeSessionId,
    DateTimeOffset SubmittedAt
) : RequestBase<Result>;