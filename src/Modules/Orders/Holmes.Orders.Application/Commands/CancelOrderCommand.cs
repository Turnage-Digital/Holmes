using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record CancelOrderCommand(
    UlidId OrderId,
    string Reason,
    DateTimeOffset CanceledAt
) : RequestBase<Result>;