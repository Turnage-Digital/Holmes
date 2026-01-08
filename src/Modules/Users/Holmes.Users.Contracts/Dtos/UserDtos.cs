using Holmes.Users.Domain;

namespace Holmes.Users.Contracts.Dtos;

public sealed record RoleAssignmentDto(
    string Id,
    UserRole Role,
    string? CustomerId,
    string GrantedBy,
    DateTimeOffset GrantedAt
);

public sealed record ExternalIdentityDto(
    string Issuer,
    string Subject,
    string? AuthenticationMethod,
    DateTimeOffset LinkedAt,
    DateTimeOffset LastSeenAt
);

public sealed record UserDto(
    string Id,
    string Email,
    string? DisplayName,
    UserStatus Status,
    DateTimeOffset LastSeenAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<RoleAssignmentDto> RoleAssignments,
    ExternalIdentityDto? ExternalIdentity
);

public sealed record UserRoleDto(UserRole Role, string? CustomerId);

public sealed record CurrentUserDto(
    string UserId,
    string Email,
    string? DisplayName,
    string Issuer,
    string Subject,
    UserStatus Status,
    DateTimeOffset LastAuthenticatedAt,
    IReadOnlyCollection<UserRoleDto> Roles
);

public sealed record InviteUserResultDto(UserDto User, string ConfirmationLink);