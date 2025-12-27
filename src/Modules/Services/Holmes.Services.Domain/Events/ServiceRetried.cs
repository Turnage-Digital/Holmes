using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceRetried(
    UlidId ServiceId,
    UlidId OrderId,
    string ServiceTypeCode,
    int AttemptCount,
    DateTimeOffset RetriedAt
) : INotification;