using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed record UpdateSubjectProfileCommand(
    UlidId TargetSubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email
) : RequestBase<Result>;

public sealed class UpdateSubjectProfileCommandHandler : IRequestHandler<UpdateSubjectProfileCommand, Result>
{
    private readonly ISubjectsUnitOfWork _unitOfWork;

    public UpdateSubjectProfileCommandHandler(
        ISubjectsUnitOfWork unitOfWork
    )
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSubjectProfileCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Subjects;
        var subject = await repository.GetByIdAsync(request.TargetSubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail($"Subject '{request.TargetSubjectId}' not found.");
        }

        subject.UpdateProfile(request.GivenName, request.FamilyName, request.DateOfBirth, request.Email);
        await repository.UpdateAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
