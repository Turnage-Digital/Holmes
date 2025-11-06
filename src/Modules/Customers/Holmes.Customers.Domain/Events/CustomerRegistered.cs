using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Customers.Domain.Events;

public sealed record CustomerRegistered(
    UlidId CustomerId,
    string Name,
    DateTimeOffset RegisteredAt
) : INotification;