using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record AcceptIntakeSubmissionCommand(
    UlidId IntakeSessionId,
    DateTimeOffset AcceptedAt
) : RequestBase<Result>;