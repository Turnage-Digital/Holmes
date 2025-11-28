namespace Holmes.Identity.Server;

public sealed class ProvisioningOptions
{
    public const string SectionName = "Provisioning";
    public string? ApiKey { get; set; }
    public string? BaseUrl { get; set; }
}