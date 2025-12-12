using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed record AddSubjectAliasCommand(
    UlidId TargetSubjectId,
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    DateTimeOffset AddedAt
) : RequestBase<Result>;

public sealed class AddSubjectAliasCommandHandler(ISubjectsUnitOfWork unitOfWork)
    : IRequestHandler<AddSubjectAliasCommand, Result>
{
    public async Task<Result> Handle(AddSubjectAliasCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Subjects;
        var subject = await repository.GetByIdAsync(request.TargetSubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail($"Subject '{request.TargetSubjectId}' not found.");
        }

        var alias = new SubjectAlias(request.GivenName, request.FamilyName, request.DateOfBirth);
        var actor = request.GetUserUlid();
        subject.AddAlias(alias, request.AddedAt, actor);
        await repository.UpdateAsync(subject, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}