using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Application.Abstractions.IntegrationEvents;

public sealed record ServicesDispatchedIntegrationEvent(
    UlidId OrderId,
    UlidId CustomerId,
    int ServiceCount,
    DateTimeOffset DispatchedAt
) : INotification;