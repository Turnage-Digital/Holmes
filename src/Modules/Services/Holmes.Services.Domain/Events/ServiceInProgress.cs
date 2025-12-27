using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceInProgress(
    UlidId ServiceId,
    UlidId OrderId,
    string ServiceTypeCode,
    string VendorCode,
    DateTimeOffset UpdatedAt
) : INotification;