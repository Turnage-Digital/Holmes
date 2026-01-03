using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Contracts.IntegrationEvents;

public sealed record ServicesDispatchedIntegrationEvent(
    UlidId OrderId,
    UlidId CustomerId,
    int ServiceCount,
    DateTimeOffset DispatchedAt
) : INotification;