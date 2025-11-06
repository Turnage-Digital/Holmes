using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Users.Domain.Events;

public sealed record UserSuspended(
    UlidId UserId,
    string Reason,
    UlidId PerformedBy,
    DateTimeOffset SuspendedAt
) : INotification;