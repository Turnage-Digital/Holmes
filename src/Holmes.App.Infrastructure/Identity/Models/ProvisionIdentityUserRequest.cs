namespace Holmes.App.Infrastructure.Security.Identity.Models;

public sealed record ProvisionIdentityUserRequest(
    string HolmesUserId,
    string Email,
    string? DisplayName
);