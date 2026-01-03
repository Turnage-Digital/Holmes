using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record StartSlaClockCommand(
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    DateTimeOffset StartedAt,
    int? TargetBusinessDays = null,
    decimal? AtRiskThresholdPercent = null
) : RequestBase<Result>;
