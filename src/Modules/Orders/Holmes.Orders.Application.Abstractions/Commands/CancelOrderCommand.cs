using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Abstractions.Commands;

public sealed record CancelOrderCommand(
    UlidId OrderId,
    string Reason,
    DateTimeOffset CanceledAt
) : RequestBase<Result>;
