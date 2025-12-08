namespace Holmes.Services.Domain;

/// <summary>
///     Represents the geographic or jurisdictional scope of a service request.
/// </summary>
public sealed record ServiceScope
{
    private ServiceScope(ServiceScopeType type, string value)
    {
        Type = type;
        Value = value;
    }

    public ServiceScopeType Type { get; }
    public string Value { get; }

    public static ServiceScope National()
    {
        return new ServiceScope(ServiceScopeType.National, "US");
    }

    public static ServiceScope State(string stateCode)
    {
        return new ServiceScope(ServiceScopeType.State, stateCode.ToUpperInvariant());
    }

    public static ServiceScope County(string fipsCode)
    {
        return new ServiceScope(ServiceScopeType.County, fipsCode);
    }

    public static ServiceScope International(string countryCode)
    {
        return new ServiceScope(ServiceScopeType.International, countryCode.ToUpperInvariant());
    }

    public override string ToString()
    {
        return $"{Type}:{Value}";
    }
}

public enum ServiceScopeType
{
    National = 0,
    State = 1,
    County = 2,
    International = 3
}