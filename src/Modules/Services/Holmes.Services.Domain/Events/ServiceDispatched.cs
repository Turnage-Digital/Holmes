using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceDispatched(
    UlidId ServiceId,
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    string VendorCode,
    string? VendorReferenceId,
    DateTimeOffset DispatchedAt
) : INotification;