using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Domain.Events;

public sealed record OrderCreated(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    DateTimeOffset CreatedAt
) : INotification;