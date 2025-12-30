using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Contracts.IntegrationEvents;

public sealed record OrderStatusChangedIntegrationEvent(
    UlidId OrderId,
    UlidId CustomerId,
    string Status,
    string Reason,
    DateTimeOffset ChangedAt
) : INotification;