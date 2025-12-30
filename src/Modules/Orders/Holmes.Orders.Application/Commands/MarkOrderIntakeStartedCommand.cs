using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record MarkOrderIntakeStartedCommand(
    UlidId OrderId,
    UlidId IntakeSessionId,
    DateTimeOffset StartedAt,
    string? Reason
) : RequestBase<Result>, ISkipUserAssignment;