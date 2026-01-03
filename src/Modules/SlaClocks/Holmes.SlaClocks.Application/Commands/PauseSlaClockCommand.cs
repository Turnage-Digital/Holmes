using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record PauseSlaClockCommand(
    UlidId ClockId,
    string Reason,
    DateTimeOffset PausedAt
) : RequestBase<Result>;
