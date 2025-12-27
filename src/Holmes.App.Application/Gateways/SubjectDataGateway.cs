using Holmes.IntakeSessions.Application.Gateways;
using Holmes.Subjects.Application.Abstractions.Commands;
using Holmes.Subjects.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Application.Gateways;

public sealed class SubjectDataGateway(
    ISender sender,
    ILogger<SubjectDataGateway> logger
) : ISubjectDataGateway
{
    public async Task UpdateSubjectIntakeDataAsync(
        SubjectIntakeDataUpdate update,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Updating subject {SubjectId} with intake data: {AddressCount} addresses, {EmploymentCount} employments, {EducationCount} educations",
            update.SubjectId,
            update.Addresses.Count,
            update.Employments.Count,
            update.Educations.Count);

        var command = new UpdateSubjectIntakeDataCommand(
            update.SubjectId,
            update.MiddleName,
            update.EncryptedSsn,
            update.SsnLast4,
            update.Addresses.Select(a => new SubjectIntakeAddress(
                    a.Street1,
                    a.Street2,
                    a.City,
                    a.State,
                    a.PostalCode,
                    a.Country,
                    a.CountyFips,
                    a.FromDate,
                    a.ToDate,
                    (AddressType)a.Type
                ))
                .ToList(),
            update.Employments.Select(e => new SubjectIntakeEmployment(
                    e.EmployerName,
                    e.EmployerPhone,
                    e.EmployerAddress,
                    e.JobTitle,
                    e.SupervisorName,
                    e.SupervisorPhone,
                    e.StartDate,
                    e.EndDate,
                    e.ReasonForLeaving,
                    e.CanContact
                ))
                .ToList(),
            update.Educations.Select(e => new SubjectIntakeEducation(
                    e.InstitutionName,
                    e.InstitutionAddress,
                    e.Degree,
                    e.Major,
                    e.AttendedFrom,
                    e.AttendedTo,
                    e.GraduationDate,
                    e.Graduated
                ))
                .ToList(),
            update.References.Select(r => new SubjectIntakeReference(
                    r.Name,
                    r.Phone,
                    r.Email,
                    r.Relationship,
                    r.YearsKnown,
                    (ReferenceType)r.Type
                ))
                .ToList(),
            update.Phones.Select(p => new SubjectIntakePhone(
                    p.PhoneNumber,
                    (PhoneType)p.Type,
                    p.IsPrimary
                ))
                .ToList(),
            update.UpdatedAt
        );

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var error = result.Error ?? "Failed to update subject intake data.";
            logger.LogWarning("Unable to update Subject {SubjectId}: {Error}", update.SubjectId, error);
            throw new InvalidOperationException(error);
        }

        logger.LogInformation("Successfully updated subject {SubjectId} with intake data", update.SubjectId);
    }
}