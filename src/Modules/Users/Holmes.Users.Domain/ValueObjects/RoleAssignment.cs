using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Users.Domain.ValueObjects;

public sealed class RoleAssignment : ValueObject
{
    public RoleAssignment(UserRole role, string? customerId, UlidId grantedBy, DateTimeOffset grantedAt)
    {
        Role = role;
        CustomerId = customerId;
        GrantedBy = grantedBy;
        GrantedAt = grantedAt;
    }

    public UserRole Role { get; }

    public string? CustomerId { get; }

    public UlidId GrantedBy { get; }

    public DateTimeOffset GrantedAt { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Role;
        yield return CustomerId;
    }
}