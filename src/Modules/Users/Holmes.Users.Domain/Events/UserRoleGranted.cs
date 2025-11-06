using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Users.Domain.Events;

public sealed record UserRoleGranted(
    UlidId UserId,
    UserRole Role,
    string? CustomerId,
    UlidId GrantedBy,
    DateTimeOffset GrantedAt
) : INotification;