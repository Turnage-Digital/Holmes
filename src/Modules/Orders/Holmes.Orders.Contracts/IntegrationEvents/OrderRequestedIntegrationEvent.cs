using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Contracts.IntegrationEvents;

public sealed record OrderRequestedIntegrationEvent(
    UlidId OrderId,
    UlidId CustomerId,
    string SubjectEmail,
    string? SubjectPhone,
    string PolicySnapshotId,
    string? PackageCode,
    DateTimeOffset OccurredAt,
    UlidId RequestedBy
) : INotification;
