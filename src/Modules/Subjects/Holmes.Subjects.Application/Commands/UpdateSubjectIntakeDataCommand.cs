using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

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

public sealed class UpdateSubjectIntakeDataCommandHandler(ISubjectsUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSubjectIntakeDataCommand, Result>
{
    public async Task<Result> Handle(UpdateSubjectIntakeDataCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Subjects;
        var subject = await repository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail($"Subject '{request.SubjectId}' not found.");
        }

        // Update middle name
        if (!string.IsNullOrEmpty(request.MiddleName))
        {
            subject.SetMiddleName(request.MiddleName);
        }

        // Update SSN
        if (request.EncryptedSsn is not null && !string.IsNullOrEmpty(request.SsnLast4))
        {
            subject.SetSsn(request.EncryptedSsn, request.SsnLast4);
        }

        // Update addresses
        var addresses = request.Addresses.Select(a => SubjectAddress.Create(
            UlidId.NewUlid(),
            a.Street1,
            a.Street2,
            a.City,
            a.State,
            a.PostalCode,
            a.Country,
            a.CountyFips,
            a.FromDate,
            a.ToDate,
            a.Type,
            request.UpdatedAt
        )).ToList();
        subject.ClearAndSetAddresses(addresses, request.UpdatedAt);

        // Update employments
        var employments = request.Employments.Select(e => SubjectEmployment.Create(
            UlidId.NewUlid(),
            e.EmployerName,
            e.EmployerPhone,
            e.EmployerAddress,
            e.JobTitle,
            e.SupervisorName,
            e.SupervisorPhone,
            e.StartDate,
            e.EndDate,
            e.ReasonForLeaving,
            e.CanContact,
            request.UpdatedAt
        )).ToList();
        subject.ClearAndSetEmployments(employments, request.UpdatedAt);

        // Update educations
        var educations = request.Educations.Select(e => SubjectEducation.Create(
            UlidId.NewUlid(),
            e.InstitutionName,
            e.InstitutionAddress,
            e.Degree,
            e.Major,
            e.AttendedFrom,
            e.AttendedTo,
            e.GraduationDate,
            e.Graduated,
            request.UpdatedAt
        )).ToList();
        subject.ClearAndSetEducations(educations, request.UpdatedAt);

        // Update references
        var references = request.References.Select(r => SubjectReference.Create(
            UlidId.NewUlid(),
            r.Name,
            r.Phone,
            r.Email,
            r.Relationship,
            r.YearsKnown,
            r.Type,
            request.UpdatedAt
        )).ToList();
        subject.ClearAndSetReferences(references);

        // Update phones
        var phones = request.Phones.Select(p => SubjectPhone.Create(
            UlidId.NewUlid(),
            p.PhoneNumber,
            p.Type,
            p.IsPrimary,
            request.UpdatedAt
        )).ToList();
        subject.ClearAndSetPhones(phones);

        await repository.UpdateAsync(subject, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
