namespace Holmes.Identity.Server;

public class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    public int ExpirationDays { get; set; } = 90;
    public int PreviousPasswordCount { get; set; } = 10;
}