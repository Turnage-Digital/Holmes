namespace Holmes.Users.Contracts;

public interface IIdentityProvisioningService
{
    Task<ProvisionedIdentityResult> ProvisionUserAsync(
        string userId,
        string email,
        string? displayName,
        CancellationToken cancellationToken
    );
}

public sealed record ProvisionedIdentityResult(string ConfirmationLink);