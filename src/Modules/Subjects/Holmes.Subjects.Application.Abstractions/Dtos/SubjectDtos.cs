namespace Holmes.Subjects.Application.Abstractions.Dtos;

public sealed record SubjectSummaryDto(
    string SubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email,
    bool IsMerged,
    int AliasCount,
    DateTimeOffset CreatedAt
);

public sealed record SubjectAliasDto(
    string Id,
    string FirstName,
    string LastName,
    DateOnly? BirthDate,
    DateTimeOffset CreatedAt
);

public sealed record SubjectListItemDto(
    string Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly? BirthDate,
    string? Email,
    string Status,
    string? MergeParentId,
    IReadOnlyCollection<SubjectAliasDto> Aliases,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

// Detail DTOs for subject history collections

public sealed record SubjectAddressDto(
    string Id,
    string Street1,
    string? Street2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? CountyFips,
    DateOnly FromDate,
    DateOnly? ToDate,
    bool IsCurrent,
    string Type,
    DateTimeOffset CreatedAt
);

public sealed record SubjectEmploymentDto(
    string Id,
    string EmployerName,
    string? EmployerPhone,
    string? EmployerAddress,
    string? JobTitle,
    string? SupervisorName,
    string? SupervisorPhone,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsCurrent,
    string? ReasonForLeaving,
    bool CanContact,
    DateTimeOffset CreatedAt
);

public sealed record SubjectEducationDto(
    string Id,
    string InstitutionName,
    string? InstitutionAddress,
    string? Degree,
    string? Major,
    DateOnly? AttendedFrom,
    DateOnly? AttendedTo,
    DateOnly? GraduationDate,
    bool Graduated,
    DateTimeOffset CreatedAt
);

public sealed record SubjectReferenceDto(
    string Id,
    string Name,
    string? Phone,
    string? Email,
    string? Relationship,
    int? YearsKnown,
    string Type,
    DateTimeOffset CreatedAt
);

public sealed record SubjectPhoneDto(
    string Id,
    string PhoneNumber,
    string Type,
    bool IsPrimary,
    DateTimeOffset CreatedAt
);

/// <summary>
///     Full subject detail including all history collections.
/// </summary>
public sealed record SubjectDetailDto(
    string Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    DateOnly? BirthDate,
    string? Email,
    string? SsnLast4,
    string Status,
    string? MergeParentId,
    IReadOnlyCollection<SubjectAliasDto> Aliases,
    IReadOnlyCollection<SubjectAddressDto> Addresses,
    IReadOnlyCollection<SubjectEmploymentDto> Employments,
    IReadOnlyCollection<SubjectEducationDto> Educations,
    IReadOnlyCollection<SubjectReferenceDto> References,
    IReadOnlyCollection<SubjectPhoneDto> Phones,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);