using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

/// <summary>
///     Transitions an order from ReadyForFulfillment to FulfillmentInProgress.
///     Called after Services have been created for the order.
/// </summary>
public sealed record BeginOrderFulfillmentCommand(
    UlidId OrderId,
    DateTimeOffset StartedAt,
    string? Reason
) : RequestBase<Result>;
