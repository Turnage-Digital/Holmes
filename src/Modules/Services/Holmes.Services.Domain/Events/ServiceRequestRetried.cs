using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceRequestRetried(
    UlidId ServiceRequestId,
    UlidId OrderId,
    string ServiceTypeCode,
    int AttemptCount,
    DateTimeOffset RetriedAt
) : INotification;
