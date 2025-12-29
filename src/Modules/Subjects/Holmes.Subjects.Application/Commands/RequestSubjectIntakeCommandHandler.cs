using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed class RequestSubjectIntakeCommandHandler(
    ISubjectsUnitOfWork unitOfWork
) : IRequestHandler<RequestSubjectIntakeCommand, Result<RequestSubjectIntakeResult>>
{
    public async Task<Result<RequestSubjectIntakeResult>> Handle(
        RequestSubjectIntakeCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.SubjectEmail))
        {
            return Result.Fail<RequestSubjectIntakeResult>("Subject email is required.");
        }

        var now = request.RequestedAt;
        var subjectWasExisting = false;

        var subject = await unitOfWork.Subjects.GetByEmailAsync(
            request.SubjectEmail,
            cancellationToken);

        if (subject is not null)
        {
            subjectWasExisting = true;

            if (!string.IsNullOrWhiteSpace(request.SubjectPhone) &&
                subject.Phones.All(p => p.PhoneNumber != request.SubjectPhone))
            {
                var phone = SubjectPhone.Create(
                    UlidId.NewUlid(),
                    request.SubjectPhone,
                    PhoneType.Mobile,
                    subject.Phones.Count == 0,
                    now);
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
                request.SubjectEmail,
                now);

            if (!string.IsNullOrWhiteSpace(request.SubjectPhone))
            {
                var phone = SubjectPhone.Create(
                    UlidId.NewUlid(),
                    request.SubjectPhone,
                    PhoneType.Mobile,
                    true,
                    now);
                subject.AddPhone(phone);
            }

            await unitOfWork.Subjects.AddAsync(subject, cancellationToken);
        }

        var orderId = UlidId.NewUlid();
        var requestedBy = request.GetUserUlid();
        subject.RequestIntake(
            orderId,
            request.CustomerId,
            request.PolicySnapshotId,
            now,
            requestedBy);

        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        return Result.Success(new RequestSubjectIntakeResult(
            subject.Id,
            subjectWasExisting,
            orderId));
    }
}