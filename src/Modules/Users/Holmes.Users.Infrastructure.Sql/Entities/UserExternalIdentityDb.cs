namespace Holmes.Users.Infrastructure.Sql.Entities;

public class UserExternalIdentityDb
{
    public long Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Issuer { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string? AuthenticationMethod { get; set; }

    public DateTimeOffset LinkedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public UserDb User { get; set; } = null!;
}