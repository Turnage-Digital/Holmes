using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record ResumeSlaClockCommand(
    UlidId ClockId,
    DateTimeOffset ResumedAt
) : RequestBase<Result>, ISkipUserAssignment;