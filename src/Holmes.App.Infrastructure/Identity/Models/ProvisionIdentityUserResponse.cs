namespace Holmes.App.Infrastructure.Security.Identity.Models;

public sealed record ProvisionIdentityUserResponse(
    string IdentityUserId,
    string Email,
    string ConfirmationLink
);