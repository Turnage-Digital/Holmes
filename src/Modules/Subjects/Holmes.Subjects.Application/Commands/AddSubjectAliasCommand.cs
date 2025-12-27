using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Commands;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

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
