using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Application.Abstractions.IntegrationEvents;

public sealed record SubjectIntakeRequestedIntegrationEvent(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    DateTimeOffset RequestedAt,
    UlidId RequestedBy
) : INotification;