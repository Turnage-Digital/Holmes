using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Services.Domain.Events;

public sealed record ServiceRequestDispatched(
    UlidId ServiceRequestId,
    UlidId OrderId,
    UlidId CustomerId,
    string ServiceTypeCode,
    string VendorCode,
    string? VendorReferenceId,
    DateTimeOffset DispatchedAt
) : INotification;