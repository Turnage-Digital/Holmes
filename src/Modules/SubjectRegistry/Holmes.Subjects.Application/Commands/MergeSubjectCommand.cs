using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed record MergeSubjectCommand(
    UlidId SourceSubjectId,
    UlidId TargetSubjectId,
    UlidId MergedBy,
    DateTimeOffset MergedAt
) : RequestBase<Result>;

public sealed class MergeSubjectCommandHandler : IRequestHandler<MergeSubjectCommand, Result>
{
    private readonly ISubjectRepository _repository;
    private readonly ISubjectsUnitOfWork _unitOfWork;

    public MergeSubjectCommandHandler(
        ISubjectRepository repository,
        ISubjectsUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MergeSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = await _repository.GetByIdAsync(request.SourceSubjectId, cancellationToken);
        if (subject is null)
        {
            return Result.Fail($"Subject '{request.SourceSubjectId}' not found.");
        }

        subject.MergeInto(request.TargetSubjectId, request.MergedBy, request.MergedAt);
        await _repository.UpdateAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}