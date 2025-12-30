using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record MarkOrderIntakeSubmittedCommand(
    UlidId OrderId,
    UlidId IntakeSessionId,
    DateTimeOffset SubmittedAt,
    string? Reason
) : RequestBase<Result>;