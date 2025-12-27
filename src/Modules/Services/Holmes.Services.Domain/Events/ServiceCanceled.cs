using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceCanceled(
    UlidId ServiceId,
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    string Reason,
    DateTimeOffset CanceledAt
) : INotification;