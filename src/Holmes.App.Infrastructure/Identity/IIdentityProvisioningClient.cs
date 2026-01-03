using Holmes.App.Infrastructure.Security.Identity.Models;

namespace Holmes.App.Infrastructure.Security.Identity;

public interface IIdentityProvisioningClient
{
    Task<ProvisionIdentityUserResponse> ProvisionUserAsync(
        ProvisionIdentityUserRequest request,
        CancellationToken cancellationToken
    );
}