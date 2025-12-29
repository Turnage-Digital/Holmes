using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record SubjectIntakeDataCaptured(
    UlidId SubjectId,
    UlidId OrderId,
    UlidId IntakeSessionId,
    string? MiddleName,
    byte[]? EncryptedSsn,
    string? SsnLast4,
    IReadOnlyList<SubjectIntakeAddressData> Addresses,
    IReadOnlyList<SubjectIntakeEmploymentData> Employments,
    IReadOnlyList<SubjectIntakeEducationData> Educations,
    IReadOnlyList<SubjectIntakeReferenceData> References,
    IReadOnlyList<SubjectIntakePhoneData> Phones,
    DateTimeOffset UpdatedAt
) : INotification;

public sealed record SubjectIntakeAddressData(
    string Street1,
    string? Street2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? CountyFips,
    DateOnly FromDate,
    DateOnly? ToDate,
    int Type
);

public sealed record SubjectIntakeEmploymentData(
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

public sealed record SubjectIntakeEducationData(
    string InstitutionName,
    string? InstitutionAddress,
    string? Degree,
    string? Major,
    DateOnly? AttendedFrom,
    DateOnly? AttendedTo,
    DateOnly? GraduationDate,
    bool Graduated
);

public sealed record SubjectIntakeReferenceData(
    string Name,
    string? Phone,
    string? Email,
    string? Relationship,
    int? YearsKnown,
    int Type
);

public sealed record SubjectIntakePhoneData(
    string PhoneNumber,
    int Type,
    bool IsPrimary
);
