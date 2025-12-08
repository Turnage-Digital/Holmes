using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Domain;

/// <summary>
/// Base type for normalized service result records.
/// Uses discriminator pattern for polymorphic storage.
/// </summary>
public abstract record NormalizedRecord
{
    public UlidId Id { get; init; }
    public string RecordType { get; init; } = null!;
    public string? SourceJurisdiction { get; init; }
    public DateTimeOffset? RecordDate { get; init; }
    public string? RawRecordHash { get; init; }
}

public enum ChargeSeverity
{
    Unknown = 0,
    Infraction = 1,
    Misdemeanor = 2,
    Felony = 3
}

public sealed record CriminalRecord : NormalizedRecord
{
    public string? CaseNumber { get; init; }
    public string? Court { get; init; }
    public string? ChargeDescription { get; init; }
    public string? ChargeCategory { get; init; }
    public ChargeSeverity? Severity { get; init; }
    public string? Disposition { get; init; }
    public DateOnly? DispositionDate { get; init; }
    public DateOnly? OffenseDate { get; init; }
    public string? Sentence { get; init; }

    public CriminalRecord()
    {
        RecordType = nameof(CriminalRecord);
    }
}

public sealed record EmploymentRecord : NormalizedRecord
{
    public string? EmployerName { get; init; }
    public string? JobTitle { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public bool? CurrentlyEmployed { get; init; }
    public string? VerificationSource { get; init; }
    public bool? Verified { get; init; }
    public string? Discrepancy { get; init; }

    public EmploymentRecord()
    {
        RecordType = nameof(EmploymentRecord);
    }
}

public sealed record EducationRecord : NormalizedRecord
{
    public string? InstitutionName { get; init; }
    public string? Degree { get; init; }
    public string? Major { get; init; }
    public DateOnly? GraduationDate { get; init; }
    public bool? Verified { get; init; }
    public string? Discrepancy { get; init; }

    public EducationRecord()
    {
        RecordType = nameof(EducationRecord);
    }
}

public sealed record IdentityRecord : NormalizedRecord
{
    public string? SsnLast4 { get; init; }
    public bool? SsnValid { get; init; }
    public bool? SsnMatch { get; init; }
    public bool? DeceasedIndicator { get; init; }
    public string? IssuingState { get; init; }
    public DateOnly? IssuedDate { get; init; }

    public IdentityRecord()
    {
        RecordType = nameof(IdentityRecord);
    }
}

public sealed record AddressRecord : NormalizedRecord
{
    public string? Street1 { get; init; }
    public string? Street2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? CountyFips { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
    public bool? IsCurrent { get; init; }

    public AddressRecord()
    {
        RecordType = nameof(AddressRecord);
    }
}

public sealed record SanctionsRecord : NormalizedRecord
{
    public string? ListName { get; init; }
    public string? MatchedName { get; init; }
    public decimal? MatchScore { get; init; }
    public string? Program { get; init; }
    public string? EntityType { get; init; }

    public SanctionsRecord()
    {
        RecordType = nameof(SanctionsRecord);
    }
}

public sealed record DrivingRecord : NormalizedRecord
{
    public string? LicenseNumber { get; init; }
    public string? LicenseState { get; init; }
    public string? LicenseClass { get; init; }
    public string? LicenseStatus { get; init; }
    public DateOnly? ExpirationDate { get; init; }
    public int? ViolationCount { get; init; }
    public int? PointsOnLicense { get; init; }

    public DrivingRecord()
    {
        RecordType = nameof(DrivingRecord);
    }
}

public sealed record DrugTestRecord : NormalizedRecord
{
    public string? TestType { get; init; }
    public string? CollectionSite { get; init; }
    public DateTimeOffset? CollectionDate { get; init; }
    public string? Result { get; init; }
    public bool? Negative { get; init; }
    public string? MroReviewStatus { get; init; }

    public DrugTestRecord()
    {
        RecordType = nameof(DrugTestRecord);
    }
}
