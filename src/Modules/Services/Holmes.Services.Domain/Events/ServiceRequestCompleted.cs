using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceRequestCompleted(
    UlidId ServiceRequestId,
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    ServiceResultStatus ResultStatus,
    int RecordCount,
    DateTimeOffset CompletedAt
) : INotification;
