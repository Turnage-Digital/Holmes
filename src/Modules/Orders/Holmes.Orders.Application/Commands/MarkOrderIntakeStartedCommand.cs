using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record MarkOrderIntakeStartedCommand(
    UlidId OrderId,
    UlidId IntakeSessionId,
    DateTimeOffset StartedAt,
    string? Reason
) : RequestBase<Result>;
