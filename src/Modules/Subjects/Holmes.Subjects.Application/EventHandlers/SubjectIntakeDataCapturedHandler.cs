using Holmes.IntakeSessions.Application.Abstractions.IntegrationEvents;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Subjects.Application.EventHandlers;

public sealed class SubjectIntakeDataCapturedHandler(
    ISender sender,
    ILogger<SubjectIntakeDataCapturedHandler> logger
) : INotificationHandler<SubjectIntakeDataCapturedIntegrationEvent>
{
    public async Task Handle(
        SubjectIntakeDataCapturedIntegrationEvent notification,
        CancellationToken cancellationToken
    )
    {
        var command = new UpdateSubjectIntakeDataCommand(
            notification.SubjectId,
            notification.MiddleName,
            notification.EncryptedSsn,
            notification.SsnLast4,
            notification.Addresses.Select(a => new SubjectIntakeAddress(
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
            notification.Employments.Select(e => new SubjectIntakeEmployment(
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
            notification.Educations.Select(e => new SubjectIntakeEducation(
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
            notification.References.Select(r => new SubjectIntakeReference(
                    r.Name,
                    r.Phone,
                    r.Email,
                    r.Relationship,
                    r.YearsKnown,
                    (ReferenceType)r.Type
                ))
                .ToList(),
            notification.Phones.Select(p => new SubjectIntakePhone(
                    p.PhoneNumber,
                    (PhoneType)p.Type,
                    p.IsPrimary
                ))
                .ToList(),
            notification.UpdatedAt
        );

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            logger.LogWarning(
                "Unable to update subject {SubjectId} intake data: {Error}",
                notification.SubjectId,
                result.Error);
        }
    }
}
