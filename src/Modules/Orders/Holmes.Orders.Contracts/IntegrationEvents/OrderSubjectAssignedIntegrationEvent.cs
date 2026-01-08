using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Orders.Contracts.IntegrationEvents;

public sealed record OrderSubjectAssignedIntegrationEvent(
    UlidId OrderId,
    UlidId CustomerId,
    UlidId SubjectId,
    DateTimeOffset OccurredAt
) : INotification;
