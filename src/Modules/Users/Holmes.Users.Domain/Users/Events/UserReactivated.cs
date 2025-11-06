using System;
using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Users.Domain.Users.Events;

public sealed record UserReactivated(
    UlidId UserId,
    UlidId PerformedBy,
    DateTimeOffset ReactivatedAt
) : INotification;
