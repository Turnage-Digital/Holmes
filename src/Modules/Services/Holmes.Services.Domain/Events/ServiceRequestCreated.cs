using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceRequestCreated(
    UlidId ServiceRequestId,
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    ServiceCategory Category,
    int Tier,
    string? ScopeType,
    string? ScopeValue,
    DateTimeOffset CreatedAt
) : INotification;
