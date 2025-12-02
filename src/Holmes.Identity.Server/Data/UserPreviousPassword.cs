namespace Holmes.Identity.Server.Data;

public class UserPreviousPassword
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
