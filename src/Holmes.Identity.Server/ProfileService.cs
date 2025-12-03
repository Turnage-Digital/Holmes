using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Holmes.Identity.Server.Data;
using IdentityModel;
using Microsoft.AspNetCore.Identity;

namespace Holmes.Identity.Server;

public sealed class ProfileService(
    UserManager<ApplicationUser> userManager,
    TimeProvider timeProvider
) : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var subjectId = context.Subject.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            return;
        }

        var user = await userManager.FindByIdAsync(subjectId);
        if (user is null)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, user.Id),
            new(JwtClaimTypes.PreferredUserName, user.UserName ?? user.Email ?? string.Empty),
            new(JwtClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add(new Claim(JwtClaimTypes.Name, user.DisplayName));
        }

        var userClaims = await userManager.GetClaimsAsync(user);
        claims.AddRange(userClaims);

        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(JwtClaimTypes.Role, role));
        }

        context.IssuedClaims.AddRange(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context)
    {
        var subjectId = context.Subject.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            context.IsActive = false;
            return;
        }

        var user = await userManager.FindByIdAsync(subjectId);
        if (user is null || !user.EmailConfirmed)
        {
            context.IsActive = false;
            return;
        }

        if (user.PasswordExpires.HasValue && user.PasswordExpires.Value < timeProvider.GetUtcNow())
        {
            context.IsActive = false;
            return;
        }

        context.IsActive = true;
    }
}