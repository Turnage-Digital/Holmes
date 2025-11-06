using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Customers.Domain.Events;

public sealed record CustomerReactivated(
    UlidId CustomerId,
    UlidId PerformedBy,
    DateTimeOffset ReactivatedAt
) : INotification;