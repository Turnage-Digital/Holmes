using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Customers.Domain.Events;

public sealed record CustomerSuspended(
    UlidId CustomerId,
    string Reason,
    UlidId PerformedBy,
    DateTimeOffset SuspendedAt
) : INotification;