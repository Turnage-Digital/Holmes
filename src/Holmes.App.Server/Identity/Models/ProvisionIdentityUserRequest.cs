namespace Holmes.App.Server.Identity.Models;

public sealed record ProvisionIdentityUserRequest(
    string HolmesUserId,
    string Email,
    string? DisplayName
);