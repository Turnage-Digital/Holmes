using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Abstractions.Commands;

public sealed record MarkOrderReadyForFulfillmentCommand(
    UlidId OrderId,
    DateTimeOffset ReadyAt,
    string? Reason
) : RequestBase<Result>;