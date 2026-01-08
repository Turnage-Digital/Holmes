using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed class CreateSubjectCommandHandler(
    ISubjectsUnitOfWork unitOfWork
) : IRequestHandler<CreateSubjectCommand, Result<CreateSubjectResult>>
{
    public async Task<Result<CreateSubjectResult>> Handle(
        CreateSubjectCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.SubjectEmail))
        {
            return Result.Fail<CreateSubjectResult>(ResultErrors.Validation);
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

        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        return Result.Success(new CreateSubjectResult(
            subject.Id,
            subjectWasExisting));
    }
}