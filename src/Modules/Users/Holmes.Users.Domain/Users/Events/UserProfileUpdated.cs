using System;
using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Users.Domain.Users.Events;

public sealed record UserProfileUpdated(
    UlidId UserId,
    string Email,
    string? DisplayName,
    DateTimeOffset UpdatedAt
) : INotification;
