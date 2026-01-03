using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Domain.Events;

public sealed record OrderStatusChanged(
    UlidId OrderId,
    UlidId CustomerId,
    OrderStatus Status,
    string Reason,
    DateTimeOffset ChangedAt
) : INotification;