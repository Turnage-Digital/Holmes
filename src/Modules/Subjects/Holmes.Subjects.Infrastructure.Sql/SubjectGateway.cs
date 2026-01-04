using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Contracts;
using Holmes.Subjects.Domain;

namespace Holmes.Subjects.Infrastructure.Sql;

public sealed class SubjectGateway(ISubjectsUnitOfWork unitOfWork) : ISubjectGateway
{
    public async Task<EnsureSubjectResult> EnsureSubjectAsync(
        string email,
        string? phone,
        DateTimeOffset requestedAt,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Subject email is required.", nameof(email));
        }

        var normalizedEmail = email.Trim();
        var normalizedPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        var subject = await unitOfWork.Subjects.GetByEmailAsync(normalizedEmail, cancellationToken);
        var subjectWasExisting = subject is not null;

        if (subject is not null)
        {
            if (!string.IsNullOrWhiteSpace(normalizedPhone) &&
                subject.Phones.All(p => p.PhoneNumber != normalizedPhone))
            {
                var phoneRecord = SubjectPhone.Create(
                    UlidId.NewUlid(),
                    normalizedPhone,
                    PhoneType.Mobile,
                    subject.Phones.Count == 0,
                    requestedAt);
                subject.AddPhone(phoneRecord);
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
                requestedAt);

            if (!string.IsNullOrWhiteSpace(normalizedPhone))
            {
                var phoneRecord = SubjectPhone.Create(
                    UlidId.NewUlid(),
                    normalizedPhone,
                    PhoneType.Mobile,
                    true,
                    requestedAt);
                subject.AddPhone(phoneRecord);
            }

            await unitOfWork.Subjects.AddAsync(subject, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        return new EnsureSubjectResult(subject.Id, subjectWasExisting);
    }
}