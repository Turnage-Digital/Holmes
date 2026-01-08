using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Domain.Events;

public sealed record OrderRequested(
    UlidId OrderId,
    UlidId CustomerId,
    string SubjectEmail,
    string? SubjectPhone,
    string PolicySnapshotId,
    string? PackageCode,
    DateTimeOffset RequestedAt,
    UlidId RequestedBy
) : INotification;
