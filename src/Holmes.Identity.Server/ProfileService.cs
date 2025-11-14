using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityModel;

namespace Holmes.Identity.Server;

internal sealed class ProfileService : IProfileService
{
    public Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var subjectId = context.Subject.FindFirst(JwtClaimTypes.Subject)?.Value;
        if (string.IsNullOrWhiteSpace(subjectId))
        {
            return Task.CompletedTask;
        }

        var user = Config.DevUsers.FirstOrDefault(u => u.SubjectId == subjectId);
        if (user is null)
        {
            return Task.CompletedTask;
        }

        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, user.SubjectId),
            new(JwtClaimTypes.Name, user.DisplayName),
            new(JwtClaimTypes.PreferredUserName, user.Username),
            new(JwtClaimTypes.Email, user.Email),
            new(JwtClaimTypes.Role, user.Role)
        };

        context.IssuedClaims.AddRange(claims);
        return Task.CompletedTask;
    }

    public Task IsActiveAsync(IsActiveContext context)
    {
        var subjectId = context.Subject.FindFirst(JwtClaimTypes.Subject)?.Value;
        context.IsActive = !string.IsNullOrWhiteSpace(subjectId) &&
                           Config.DevUsers.Any(u => u.SubjectId == subjectId);
        return Task.CompletedTask;
    }
}