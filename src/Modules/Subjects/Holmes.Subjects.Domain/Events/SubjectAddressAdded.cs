using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Domain.Events;

public sealed record SubjectAddressAdded(
    UlidId SubjectId,
    UlidId AddressId,
    string City,
    string State,
    DateOnly FromDate,
    DateOnly? ToDate,
    DateTimeOffset Timestamp) : INotification;
