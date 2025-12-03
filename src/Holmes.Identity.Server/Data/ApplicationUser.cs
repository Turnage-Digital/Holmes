using Microsoft.AspNetCore.Identity;

namespace Holmes.Identity.Server.Data;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTimeOffset? PasswordExpires { get; set; }
    public DateTimeOffset? LastPasswordChangedAt { get; set; }

    public ICollection<UserPreviousPassword> PreviousPasswords { get; set; } = [];
}