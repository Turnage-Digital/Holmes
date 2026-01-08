using Holmes.Core.Application;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed class UpdateSubjectProfileCommandHandler(ISubjectsUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSubjectProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateSubjectProfileCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Subjects;
        var subject = await repository.GetByIdAsync(request.TargetSubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        subject.UpdateProfile(request.GivenName, request.FamilyName, request.DateOfBirth, request.Email);
        await repository.UpdateAsync(subject, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}