using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Customers.Domain.Events;

public sealed record CustomerAdminAssigned(
    UlidId CustomerId,
    UlidId UserId,
    UlidId AssignedBy,
    DateTimeOffset AssignedAt
) : INotification;