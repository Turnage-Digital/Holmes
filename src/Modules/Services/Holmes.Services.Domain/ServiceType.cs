namespace Holmes.Services.Domain;

/// <summary>
///     Represents a specific type of background check service.
/// </summary>
public sealed record ServiceType
{
    // Identity services (Tier 1)
    public static readonly ServiceType SsnTrace = new(ServiceCategory.Identity, "SSN_TRACE", "SSN Trace", 1);

    public static readonly ServiceType SsnVerification =
        new(ServiceCategory.Identity, "SSN_VERIFY", "SSN Verification", 1);

    public static readonly ServiceType AddressVerification =
        new(ServiceCategory.Identity, "ADDR_VERIFY", "Address Verification", 1);

    public static readonly ServiceType IdentityVerification =
        new(ServiceCategory.Identity, "ID_VERIFY", "Identity Verification", 1);

    public static readonly ServiceType DeathMasterFile = new(ServiceCategory.Identity, "DMF", "Death Master File", 1);
    public static readonly ServiceType OfacSanctions = new(ServiceCategory.Identity, "OFAC", "OFAC Sanctions", 1);

    // Criminal services (Tier 2)
    public static readonly ServiceType FederalCriminal =
        new(ServiceCategory.Criminal, "FED_CRIM", "Federal Criminal", 2);

    public static readonly ServiceType StatewideSearch =
        new(ServiceCategory.Criminal, "STATE_CRIM", "Statewide Criminal", 2);

    public static readonly ServiceType
        CountySearch = new(ServiceCategory.Criminal, "COUNTY_CRIM", "County Criminal", 2);

    public static readonly ServiceType MunicipalSearch =
        new(ServiceCategory.Criminal, "MUNI_CRIM", "Municipal Criminal", 2);

    public static readonly ServiceType SexOffenderRegistry =
        new(ServiceCategory.Criminal, "SEX_OFF", "Sex Offender Registry", 2);

    public static readonly ServiceType GlobalWatchlist =
        new(ServiceCategory.Criminal, "WATCHLIST", "Global Watchlist", 2);

    // Employment/Education services (Tier 3)
    public static readonly ServiceType TwnEmployment =
        new(ServiceCategory.Employment, "TWN_EMP", "TWN Employment Verification", 3);

    public static readonly ServiceType DirectEmployment =
        new(ServiceCategory.Employment, "DIRECT_EMP", "Direct Employment Verification", 3);

    public static readonly ServiceType IncomeVerification =
        new(ServiceCategory.Employment, "INCOME_VERIFY", "Income Verification", 3);

    public static readonly ServiceType EducationVerification =
        new(ServiceCategory.Education, "EDU_VERIFY", "Education Verification", 3);

    public static readonly ServiceType ProfessionalLicense =
        new(ServiceCategory.Education, "PROF_LICENSE", "Professional License", 3);

    // Expensive services (Tier 4)
    public static readonly ServiceType DrugTest = new(ServiceCategory.Drug, "DRUG_TEST", "Drug Test", 4);
    public static readonly ServiceType Mvr = new(ServiceCategory.Driving, "MVR", "Motor Vehicle Record", 4);

    public static readonly ServiceType CdlVerification =
        new(ServiceCategory.Driving, "CDL_VERIFY", "CDL Verification", 4);

    public static readonly ServiceType CreditCheck = new(ServiceCategory.Credit, "CREDIT", "Credit Check", 4);

    // Civil services
    public static readonly ServiceType CivilSearch = new(ServiceCategory.Civil, "CIVIL", "Civil Records Search", 3);

    // Reference services
    public static readonly ServiceType ProfessionalReference =
        new(ServiceCategory.Reference, "PROF_REF", "Professional Reference", 3);

    private static readonly Dictionary<string, ServiceType> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        [SsnTrace.Code] = SsnTrace,
        [SsnVerification.Code] = SsnVerification,
        [AddressVerification.Code] = AddressVerification,
        [IdentityVerification.Code] = IdentityVerification,
        [DeathMasterFile.Code] = DeathMasterFile,
        [OfacSanctions.Code] = OfacSanctions,
        [FederalCriminal.Code] = FederalCriminal,
        [StatewideSearch.Code] = StatewideSearch,
        [CountySearch.Code] = CountySearch,
        [MunicipalSearch.Code] = MunicipalSearch,
        [SexOffenderRegistry.Code] = SexOffenderRegistry,
        [GlobalWatchlist.Code] = GlobalWatchlist,
        [TwnEmployment.Code] = TwnEmployment,
        [DirectEmployment.Code] = DirectEmployment,
        [IncomeVerification.Code] = IncomeVerification,
        [EducationVerification.Code] = EducationVerification,
        [ProfessionalLicense.Code] = ProfessionalLicense,
        [DrugTest.Code] = DrugTest,
        [Mvr.Code] = Mvr,
        [CdlVerification.Code] = CdlVerification,
        [CreditCheck.Code] = CreditCheck,
        [CivilSearch.Code] = CivilSearch,
        [ProfessionalReference.Code] = ProfessionalReference
    };

    private ServiceType(ServiceCategory category, string code, string displayName, int defaultTier)
    {
        Category = category;
        Code = code;
        DisplayName = displayName;
        DefaultTier = defaultTier;
    }

    public ServiceCategory Category { get; }
    public string Code { get; }
    public string DisplayName { get; }
    public int DefaultTier { get; }

    public static IEnumerable<ServiceType> All => ByCode.Values;

    public static ServiceType? FromCode(string code)
    {
        return ByCode.TryGetValue(code, out var type) ? type : null;
    }

    public static IEnumerable<ServiceType> ByCategory(ServiceCategory category)
    {
        return ByCode.Values.Where(t => t.Category == category);
    }

    public override string ToString()
    {
        return Code;
    }
}