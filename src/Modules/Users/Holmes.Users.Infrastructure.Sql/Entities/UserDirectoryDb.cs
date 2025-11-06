using Holmes.Users.Domain;

namespace Holmes.Users.Infrastructure.Sql.Entities;

public class UserDirectoryDb
{
    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string Issuer { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public DateTimeOffset LastAuthenticatedAt { get; set; }

    public UserStatus Status { get; set; }
}