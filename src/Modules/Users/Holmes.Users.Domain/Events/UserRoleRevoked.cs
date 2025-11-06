using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Users.Domain.Events;

public sealed record UserRoleRevoked(
    UlidId UserId,
    UserRole Role,
    string? CustomerId,
    UlidId RevokedBy,
    DateTimeOffset RevokedAt
) : INotification;