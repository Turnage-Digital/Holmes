using System;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain.Users;
using MediatR;

namespace Holmes.Users.Domain.Users.Events;

public sealed record UserRoleGranted(
    UlidId UserId,
    UserRole Role,
    string? CustomerId,
    UlidId GrantedBy,
    DateTimeOffset GrantedAt
) : INotification;
