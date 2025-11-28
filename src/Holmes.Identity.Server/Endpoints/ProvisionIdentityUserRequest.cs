using System.ComponentModel.DataAnnotations;

namespace Holmes.Identity.Server.Endpoints;

public sealed class ProvisionIdentityUserRequest
{
    [Required]
    public string? HolmesUserId { get; init; }

    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    public string? DisplayName { get; init; }

    public string? ConfirmationReturnUrl { get; init; }
}