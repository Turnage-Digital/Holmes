using Holmes.App.Infrastructure.Identity.Models;

namespace Holmes.App.Infrastructure.Identity;

public interface IIdentityProvisioningClient
{
    Task<ProvisionIdentityUserResponse> ProvisionUserAsync(
        ProvisionIdentityUserRequest request,
        CancellationToken cancellationToken
    );
}