using Holmes.IntakeSessions.Contracts.Services;

namespace Holmes.IntakeSessions.Application.Services;

/// <summary>
///     Default implementation of service-to-section mapping.
///     Note: Addresses are always required (regulatory minimum) and handled separately in the frontend.
/// </summary>
public sealed class IntakeSectionMappingService : IIntakeSectionMappingService
{
    // Data-driven mapping: Service code -> required intake sections
    // This makes it easy to add new services without code changes elsewhere
    private static readonly IReadOnlyDictionary<string, string[]> ServiceToSectionMap =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Employment verification services require employment history
            ["TWN_EMP"] = [IntakeSections.Employment],
            ["DIRECT_EMP"] = [IntakeSections.Employment],
            ["INCOME_VERIFY"] = [IntakeSections.Employment],

            // Education verification requires education history
            ["EDU_VERIFY"] = [IntakeSections.Education],
            ["PROF_LICENSE"] = [IntakeSections.Education],

            // Reference check requires references
            ["PROF_REF"] = [IntakeSections.References],

            // Drug test requires phone for scheduling
            ["DRUG_TEST"] = [IntakeSections.Phone],

            // MVR/CDL could require driving info in future
            ["MVR"] = [IntakeSections.DrivingInfo],
            ["CDL_VERIFY"] = [IntakeSections.DrivingInfo]
        };

    public IReadOnlySet<string> GetRequiredSections(IEnumerable<string> serviceTypeCodes)
    {
        var sections = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in serviceTypeCodes)
        {
            if (ServiceToSectionMap.TryGetValue(code, out var mappedSections))
            {
                foreach (var section in mappedSections)
                {
                    sections.Add(section);
                }
            }
        }

        return sections;
    }
}