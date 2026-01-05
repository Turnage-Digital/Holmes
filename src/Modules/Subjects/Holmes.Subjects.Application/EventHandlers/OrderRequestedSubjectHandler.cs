using Holmes.Core.Domain.ValueObjects;
using Holmes.Orders.Contracts.IntegrationEvents;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.EventHandlers;

public sealed class OrderRequestedSubjectHandler(
    ISubjectsUnitOfWork unitOfWork
) : INotificationHandler<OrderRequestedIntegrationEvent>
{
    public async Task Handle(OrderRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(notification.SubjectEmail))
        {
            return;
        }

        var normalizedEmail = notification.SubjectEmail.Trim().ToLowerInvariant();
        var normalizedPhone = string.IsNullOrWhiteSpace(notification.SubjectPhone)
            ? null
            : notification.SubjectPhone.Trim();

        var subjectWasExisting = false;
        var subject = await unitOfWork.Subjects.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (subject is not null)
        {
            subjectWasExisting = true;

            if (!string.IsNullOrWhiteSpace(normalizedPhone) &&
                subject.Phones.All(p => p.PhoneNumber != normalizedPhone))
            {
                var phone = SubjectPhone.Create(
                    UlidId.NewUlid(),
                    normalizedPhone,
                    PhoneType.Mobile,
                    subject.Phones.Count == 0,
                    notification.OccurredAt);
                subject.AddPhone(phone);
                await unitOfWork.Subjects.UpdateAsync(subject, cancellationToken);
            }
        }
        else
        {
            subject = Subject.Register(
                UlidId.NewUlid(),
                "",
                "",
                null,
                normalizedEmail,
                notification.OccurredAt);

            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                var phone = SubjectPhone.Create(
                    UlidId.NewUlid(),
                    normalizedPhone,
                    PhoneType.Mobile,
                    true,
                    notification.OccurredAt);
                subject.AddPhone(phone);
            }

            await unitOfWork.Subjects.AddAsync(subject, cancellationToken);
        }

        subject.RecordResolution(
            notification.OrderId,
            notification.CustomerId,
            notification.OccurredAt,
            subjectWasExisting);

        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
