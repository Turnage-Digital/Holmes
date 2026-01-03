using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;

namespace Holmes.Subjects.Application.Commands;

public sealed record UpdateSubjectIntakeDataCommand(
    UlidId SubjectId,
    string? MiddleName,
    byte[]? EncryptedSsn,
    string? SsnLast4,
    IReadOnlyList<SubjectIntakeAddress> Addresses,
    IReadOnlyList<SubjectIntakeEmployment> Employments,
    IReadOnlyList<SubjectIntakeEducation> Educations,
    IReadOnlyList<SubjectIntakeReference> References,
    IReadOnlyList<SubjectIntakePhone> Phones,
    DateTimeOffset UpdatedAt
) : RequestBase<Result>;

public sealed record SubjectIntakeAddress(
    string Street1,
    string? Street2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? CountyFips,
    DateOnly FromDate,
    DateOnly? ToDate,
    AddressType Type
);

public sealed record SubjectIntakeEmployment(
    string EmployerName,
    string? EmployerPhone,
    string? EmployerAddress,
    string? JobTitle,
    string? SupervisorName,
    string? SupervisorPhone,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? ReasonForLeaving,
    bool CanContact
);

public sealed record SubjectIntakeEducation(
    string InstitutionName,
    string? InstitutionAddress,
    string? Degree,
    string? Major,
    DateOnly? AttendedFrom,
    DateOnly? AttendedTo,
    DateOnly? GraduationDate,
    bool Graduated
);

public sealed record SubjectIntakeReference(
    string Name,
    string? Phone,
    string? Email,
    string? Relationship,
    int? YearsKnown,
    ReferenceType Type
);

public sealed record SubjectIntakePhone(
    string PhoneNumber,
    PhoneType Type,
    bool IsPrimary
);