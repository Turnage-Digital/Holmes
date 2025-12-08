using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceRequestCanceled(
    UlidId ServiceRequestId,
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    string Reason,
    DateTimeOffset CanceledAt
) : INotification;
