using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Domain.Events;

public sealed record OrderCreatedFromIntake(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    DateTimeOffset CreatedAt,
    UlidId RequestedBy
) : INotification;
