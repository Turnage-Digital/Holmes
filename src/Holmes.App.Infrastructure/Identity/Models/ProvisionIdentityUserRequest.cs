namespace Holmes.App.Infrastructure.Identity.Models;

public sealed record ProvisionIdentityUserRequest(
    string HolmesUserId,
    string Email,
    string? DisplayName
);
