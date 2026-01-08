namespace Holmes.IntakeSessions.Domain;

public static class IntakeMetadataKeys
{
    public const string DisclosureId = "disclosureId";
    public const string DisclosureVersion = "disclosureVersion";
    public const string DisclosureHash = "disclosureHash";
    public const string DisclosureFormat = "disclosureFormat";
    public const string DisclosureContent = "disclosureContent";

    public const string AuthorizationId = "authorizationId";
    public const string AuthorizationVersion = "authorizationVersion";
    public const string AuthorizationHash = "authorizationHash";
    public const string AuthorizationFormat = "authorizationFormat";
    public const string AuthorizationContent = "authorizationContent";
    public const string AuthorizationMode = "authorizationMode";

    public const string ClientIpAddress = "ipAddress";
    public const string ClientUserAgent = "userAgent";
    public const string ClientCapturedAt = "clientCapturedAt";
    public const string ServerReceivedAt = "receivedAt";
}
