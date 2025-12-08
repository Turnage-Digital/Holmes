namespace Holmes.Services.Domain;

/// <summary>
/// Represents the geographic or jurisdictional scope of a service request.
/// </summary>
public sealed record ServiceScope
{
    public ServiceScopeType Type { get; }
    public string Value { get; }

    private ServiceScope(ServiceScopeType type, string value)
    {
        Type = type;
        Value = value;
    }

    public static ServiceScope National() => new(ServiceScopeType.National, "US");
    public static ServiceScope State(string stateCode) => new(ServiceScopeType.State, stateCode.ToUpperInvariant());
    public static ServiceScope County(string fipsCode) => new(ServiceScopeType.County, fipsCode);
    public static ServiceScope International(string countryCode) => new(ServiceScopeType.International, countryCode.ToUpperInvariant());

    public override string ToString() => $"{Type}:{Value}";
}

public enum ServiceScopeType
{
    National = 0,
    State = 1,
    County = 2,
    International = 3
}
