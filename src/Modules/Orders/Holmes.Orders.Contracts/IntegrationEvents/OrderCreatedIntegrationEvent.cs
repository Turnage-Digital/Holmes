using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Contracts.IntegrationEvents;

public sealed record OrderCreatedIntegrationEvent(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    DateTimeOffset CreatedAt,
    UlidId CreatedBy
) : INotification;
