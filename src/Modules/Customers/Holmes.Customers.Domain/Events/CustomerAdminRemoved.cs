using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Customers.Domain.Events;

public sealed record CustomerAdminRemoved(
    UlidId CustomerId,
    UlidId UserId,
    UlidId RemovedBy,
    DateTimeOffset RemovedAt
) : INotification;