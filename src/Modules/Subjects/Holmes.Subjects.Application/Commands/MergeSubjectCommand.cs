using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed record MergeSubjectCommand(
    UlidId SourceSubjectId,
    UlidId TargetSubjectId,
    DateTimeOffset MergedAt
) : RequestBase<Result>;

public sealed class MergeSubjectCommandHandler(ISubjectsUnitOfWork unitOfWork)
    : IRequestHandler<MergeSubjectCommand, Result>
{
    public async Task<Result> Handle(MergeSubjectCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Subjects;
        var subject = await repository.GetByIdAsync(request.SourceSubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail($"Subject '{request.SourceSubjectId}' not found.");
        }

        var actor = request.GetUserUlid();
        subject.MergeInto(request.TargetSubjectId, actor, request.MergedAt);
        await repository.UpdateAsync(subject, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}