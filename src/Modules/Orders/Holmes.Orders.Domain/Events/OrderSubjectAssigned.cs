using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Domain.Events;

public sealed record OrderSubjectAssigned(
    UlidId OrderId,
    UlidId CustomerId,
    UlidId SubjectId,
    DateTimeOffset AssignedAt
) : INotification;
