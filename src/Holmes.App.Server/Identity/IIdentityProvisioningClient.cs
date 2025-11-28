using Holmes.App.Server.Identity.Models;

namespace Holmes.App.Server.Identity;

public interface IIdentityProvisioningClient
{
    Task<ProvisionIdentityUserResponse> ProvisionUserAsync(
        ProvisionIdentityUserRequest request,
        CancellationToken cancellationToken
    );
}