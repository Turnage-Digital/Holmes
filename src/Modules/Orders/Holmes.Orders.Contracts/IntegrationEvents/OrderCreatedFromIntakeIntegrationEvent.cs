using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Contracts.IntegrationEvents;

public sealed record OrderCreatedFromIntakeIntegrationEvent(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    DateTimeOffset CreatedAt,
    UlidId RequestedBy
) : INotification;