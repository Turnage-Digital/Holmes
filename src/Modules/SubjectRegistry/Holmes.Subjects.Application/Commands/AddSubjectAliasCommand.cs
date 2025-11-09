using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed record AddSubjectAliasCommand(
    UlidId TargetSubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    DateTimeOffset AddedAt
) : RequestBase<Result>;

public sealed class AddSubjectAliasCommandHandler : IRequestHandler<AddSubjectAliasCommand, Result>
{
    private readonly ISubjectsUnitOfWork _unitOfWork;

    public AddSubjectAliasCommandHandler(
        ISubjectsUnitOfWork unitOfWork
    )
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddSubjectAliasCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Subjects;
        var subject = await repository.GetByIdAsync(request.TargetSubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail($"Subject '{request.TargetSubjectId}' not found.");
        }

        var alias = new SubjectAlias(request.GivenName, request.FamilyName, request.DateOfBirth);
        var actor = request.GetUserUlid();
        subject.AddAlias(alias, request.AddedAt, actor);
        await repository.UpdateAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
