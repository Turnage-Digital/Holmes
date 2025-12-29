using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Abstractions.IntegrationEvents;

public sealed record ServiceCompletedIntegrationEvent(
    UlidId ServiceId,
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    ServiceResultStatus ResultStatus,
    int RecordCount,
    DateTimeOffset CompletedAt
) : INotification;