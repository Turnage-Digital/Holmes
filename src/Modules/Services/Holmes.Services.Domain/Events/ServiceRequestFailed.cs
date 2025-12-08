using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceRequestFailed(
    UlidId ServiceRequestId,
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    string ErrorMessage,
    int AttemptCount,
    int MaxAttempts,
    bool WillRetry,
    DateTimeOffset FailedAt
) : INotification;