using Microsoft.AspNetCore.Identity;

namespace Holmes.Identity.Server.Data;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}