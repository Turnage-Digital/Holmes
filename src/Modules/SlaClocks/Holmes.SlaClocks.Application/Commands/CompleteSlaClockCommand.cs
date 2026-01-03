using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record CompleteSlaClockCommand(
    UlidId OrderId,
    ClockKind Kind,
    DateTimeOffset CompletedAt
) : RequestBase<Result>;
