using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceRequestInProgress(
    UlidId ServiceRequestId,
    UlidId OrderId,
    string ServiceTypeCode,
    string VendorCode,
    DateTimeOffset UpdatedAt
) : INotification;
