using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record MarkClockAtRiskCommand(
    UlidId ClockId,
    DateTimeOffset AtRiskAt
) : RequestBase<Result>, ISkipUserAssignment;