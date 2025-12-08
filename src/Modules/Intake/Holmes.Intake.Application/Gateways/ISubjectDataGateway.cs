using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Intake.Application.Gateways;

public interface ISubjectDataGateway
{
    Task UpdateSubjectIntakeDataAsync(
        SubjectIntakeDataUpdate update,
        CancellationToken cancellationToken
    );
}

public sealed record SubjectIntakeDataUpdate(
    UlidId SubjectId,
    string? MiddleName,
    byte[]? EncryptedSsn,
    string? SsnLast4,
    IReadOnlyList<IntakeAddressDto> Addresses,
    IReadOnlyList<IntakeEmploymentDto> Employments,
    IReadOnlyList<IntakeEducationDto> Educations,
    IReadOnlyList<IntakeReferenceDto> References,
    IReadOnlyList<IntakePhoneDto> Phones,
    DateTimeOffset UpdatedAt
);

public sealed record IntakeAddressDto(
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

public sealed record IntakeEmploymentDto(
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

public sealed record IntakeEducationDto(
    string InstitutionName,
    string? InstitutionAddress,
    string? Degree,
    string? Major,
    DateOnly? AttendedFrom,
    DateOnly? AttendedTo,
    DateOnly? GraduationDate,
    bool Graduated
);

public sealed record IntakeReferenceDto(
    string Name,
    string? Phone,
    string? Email,
    string? Relationship,
    int? YearsKnown,
    int Type
);

public sealed record IntakePhoneDto(
    string PhoneNumber,
    int Type,
    bool IsPrimary
);