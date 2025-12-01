namespace Holmes.App.Infrastructure.Identity.Models;

public sealed record ProvisionIdentityUserResponse(
    string IdentityUserId,
    string Email,
    string ConfirmationLink
);
