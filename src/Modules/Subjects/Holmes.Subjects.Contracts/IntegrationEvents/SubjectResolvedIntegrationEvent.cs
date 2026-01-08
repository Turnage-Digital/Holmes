using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Contracts.IntegrationEvents;

public sealed record SubjectResolvedIntegrationEvent(
    UlidId OrderId,
    UlidId CustomerId,
    UlidId SubjectId,
    DateTimeOffset OccurredAt,
    bool WasExisting
) : INotification;
