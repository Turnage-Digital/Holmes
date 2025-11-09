using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed record MergeSubjectCommand(
    UlidId SourceSubjectId,
    UlidId TargetSubjectId,
    DateTimeOffset MergedAt
) : RequestBase<Result>;

public sealed class MergeSubjectCommandHandler : IRequestHandler<MergeSubjectCommand, Result>
{
    private readonly ISubjectsUnitOfWork _unitOfWork;

    public MergeSubjectCommandHandler(
        ISubjectsUnitOfWork unitOfWork
    )
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MergeSubjectCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Subjects;
        var subject = await repository.GetByIdAsync(request.SourceSubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail($"Subject '{request.SourceSubjectId}' not found.");
        }

        var actor = request.GetUserUlid();
        subject.MergeInto(request.TargetSubjectId, actor, request.MergedAt);
        await repository.UpdateAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
