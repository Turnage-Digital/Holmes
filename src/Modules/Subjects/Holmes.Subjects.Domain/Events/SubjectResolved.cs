using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Domain.Events;

public sealed record SubjectResolved(
    UlidId OrderId,
    UlidId CustomerId,
    UlidId SubjectId,
    DateTimeOffset ResolvedAt,
    bool WasExisting
) : INotification;
