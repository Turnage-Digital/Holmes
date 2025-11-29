namespace Holmes.App.Server.Identity.Models;

public sealed record ProvisionIdentityUserResponse(
    string IdentityUserId,
    string Email,
    string ConfirmationLink
);