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
    private readonly ISubjectRepository _repository;
    private readonly ISubjectsUnitOfWork _unitOfWork;

    public UpdateSubjectProfileCommandHandler(
        ISubjectRepository repository,
        ISubjectsUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateSubjectProfileCommand request, CancellationToken cancellationToken)
    {
        var subject = await _repository.GetByIdAsync(request.TargetSubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail($"Subject '{request.TargetSubjectId}' not found.");
        }

        subject.UpdateProfile(request.GivenName, request.FamilyName, request.DateOfBirth, request.Email);
        await _repository.UpdateAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}