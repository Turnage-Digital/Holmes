using System;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain.Users;
using MediatR;

namespace Holmes.Users.Domain.Users.Events;

public sealed record UserRoleRevoked(
    UlidId UserId,
    UserRole Role,
    string? CustomerId,
    UlidId RevokedBy,
    DateTimeOffset RevokedAt
) : INotification;
