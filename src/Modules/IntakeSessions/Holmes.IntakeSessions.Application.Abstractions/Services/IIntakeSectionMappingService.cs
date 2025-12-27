namespace Holmes.IntakeSessions.Application.Abstractions.Services;

/// <summary>
///     Maps service type codes to required intake form sections.
/// </summary>
public interface IIntakeSectionMappingService
{
    /// <summary>
    ///     Given a list of ordered service type codes, returns the set of
    ///     intake sections that must be displayed to collect necessary data.
    /// </summary>
    IReadOnlySet<string> GetRequiredSections(IEnumerable<string> serviceTypeCodes);
}

/// <summary>
///     Represents sections of the intake form that can be conditionally shown
///     based on ordered services.
/// </summary>
public static class IntakeSections
{
    public const string Employment = "Employment";
    public const string Education = "Education";
    public const string References = "References";
    public const string Phone = "Phone";
    public const string DrivingInfo = "DrivingInfo";
}