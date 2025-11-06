using System;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain.Users;

namespace Holmes.Users.Infrastructure.Sql.Entities;

public class UserRoleMembership
{
    public long Id { get; set; }

    public string UserId { get; set; } = null!;

    public UserRole Role { get; set; }

    public string? CustomerId { get; set; }

    public UlidId GrantedBy { get; set; }

    public DateTimeOffset GrantedAt { get; set; }
}
