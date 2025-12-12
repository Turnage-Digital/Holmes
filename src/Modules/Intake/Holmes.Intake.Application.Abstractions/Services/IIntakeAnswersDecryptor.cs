using Holmes.Intake.Domain.ValueObjects;

namespace Holmes.Intake.Application.Abstractions.Services;

public interface IIntakeAnswersDecryptor
{
    Task<DecryptedIntakeAnswers?> DecryptAsync(
        IntakeAnswersSnapshot snapshot,
        CancellationToken cancellationToken
    );
}

public sealed record DecryptedIntakeAnswers(
    string? MiddleName,
    string? Ssn,
    IReadOnlyList<DecryptedAddress> Addresses,
    IReadOnlyList<DecryptedEmployment> Employments,
    IReadOnlyList<DecryptedEducation> Educations,
    IReadOnlyList<DecryptedReference> References,
    IReadOnlyList<DecryptedPhone> Phones
);

public sealed record DecryptedAddress(
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

public sealed record DecryptedEmployment(
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

public sealed record DecryptedEducation(
    string InstitutionName,
    string? InstitutionAddress,
    string? Degree,
    string? Major,
    DateOnly? AttendedFrom,
    DateOnly? AttendedTo,
    DateOnly? GraduationDate,
    bool Graduated
);

public sealed record DecryptedReference(
    string Name,
    string? Phone,
    string? Email,
    string? Relationship,
    int? YearsKnown,
    int Type
);

public sealed record DecryptedPhone(
    string PhoneNumber,
    int Type,
    bool IsPrimary
);