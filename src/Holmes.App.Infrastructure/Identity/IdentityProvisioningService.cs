using Holmes.App.Infrastructure.Security.Identity.Models;
using Holmes.Users.Contracts;

namespace Holmes.App.Infrastructure.Security.Identity;

public sealed class IdentityProvisioningService(
    IIdentityProvisioningClient client
) : IIdentityProvisioningService
{
    public async Task<ProvisionedIdentityResult> ProvisionUserAsync(
        string userId,
        string email,
        string? displayName,
        CancellationToken cancellationToken
    )
    {
        var response = await client.ProvisionUserAsync(
            new ProvisionIdentityUserRequest(userId, email, displayName),
            cancellationToken);

        return new ProvisionedIdentityResult(response.ConfirmationLink);
    }
}