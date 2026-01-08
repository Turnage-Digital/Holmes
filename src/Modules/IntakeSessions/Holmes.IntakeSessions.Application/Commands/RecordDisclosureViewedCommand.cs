using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record RecordDisclosureViewedCommand(
    UlidId IntakeSessionId,
    DateTimeOffset ViewedAt
) : RequestBase<Result>;
