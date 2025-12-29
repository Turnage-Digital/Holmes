using Holmes.IntakeSessions.Application.Abstractions.IntegrationEvents;
using Holmes.IntakeSessions.Domain.Events;
using MediatR;

namespace Holmes.IntakeSessions.Application.EventHandlers;

public sealed class IntakeSessionIntegrationEventPublisher(
    IMediator mediator
) : INotificationHandler<IntakeSessionInvited>,
    INotificationHandler<IntakeSessionStarted>,
    INotificationHandler<IntakeSubmissionReceived>,
    INotificationHandler<IntakeSubmissionAccepted>,
    INotificationHandler<SubjectIntakeDataCaptured>
{
    public Task Handle(IntakeSessionInvited notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new IntakeSessionInvitedIntegrationEvent(
            notification.IntakeSessionId,
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId,
            notification.ResumeToken,
            notification.InvitedAt,
            notification.ExpiresAt,
            notification.PolicySnapshot), cancellationToken);
    }

    public Task Handle(IntakeSessionStarted notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new IntakeSessionStartedIntegrationEvent(
            notification.IntakeSessionId,
            notification.OrderId,
            notification.StartedAt,
            notification.DeviceInfo), cancellationToken);
    }

    public Task Handle(IntakeSubmissionReceived notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new IntakeSubmissionReceivedIntegrationEvent(
            notification.IntakeSessionId,
            notification.OrderId,
            notification.SubmittedAt), cancellationToken);
    }

    public Task Handle(IntakeSubmissionAccepted notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new IntakeSubmissionAcceptedIntegrationEvent(
            notification.IntakeSessionId,
            notification.OrderId,
            notification.AcceptedAt), cancellationToken);
    }

    public Task Handle(SubjectIntakeDataCaptured notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new SubjectIntakeDataCapturedIntegrationEvent(
            notification.SubjectId,
            notification.OrderId,
            notification.IntakeSessionId,
            notification.MiddleName,
            notification.EncryptedSsn,
            notification.SsnLast4,
            notification.Addresses.Select(a => new Application.Abstractions.IntegrationEvents.SubjectIntakeAddressData(
                    a.Street1,
                    a.Street2,
                    a.City,
                    a.State,
                    a.PostalCode,
                    a.Country,
                    a.CountyFips,
                    a.FromDate,
                    a.ToDate,
                    a.Type
                ))
                .ToList(),
            notification.Employments.Select(e => new Application.Abstractions.IntegrationEvents.SubjectIntakeEmploymentData(
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
            notification.Educations.Select(e => new Application.Abstractions.IntegrationEvents.SubjectIntakeEducationData(
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
            notification.References.Select(r => new Application.Abstractions.IntegrationEvents.SubjectIntakeReferenceData(
                    r.Name,
                    r.Phone,
                    r.Email,
                    r.Relationship,
                    r.YearsKnown,
                    r.Type
                ))
                .ToList(),
            notification.Phones.Select(p => new Application.Abstractions.IntegrationEvents.SubjectIntakePhoneData(
                    p.PhoneNumber,
                    p.Type,
                    p.IsPrimary
                ))
                .ToList(),
            notification.UpdatedAt), cancellationToken);
    }
}
