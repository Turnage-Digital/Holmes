using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record MarkOrderIntakeSubmittedCommand(
    UlidId OrderId,
    UlidId IntakeSessionId,
    DateTimeOffset SubmittedAt,
    string? Reason
) : RequestBase<Result>;