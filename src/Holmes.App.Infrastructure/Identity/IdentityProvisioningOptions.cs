namespace Holmes.App.Infrastructure.Identity;

public sealed class IdentityProvisioningOptions
{
    public const string SectionName = "IdentityProvisioning";

    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? ConfirmationReturnUrl { get; set; }
}
