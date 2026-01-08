using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Contracts;
using Holmes.Subjects.Contracts.Dtos;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed class RegisterSubjectCommandHandler(
    ISubjectsUnitOfWork unitOfWork,
    ISubjectQueries subjectQueries
) : IRequestHandler<RegisterSubjectCommand, Result<SubjectSummaryDto>>
{
    public async Task<Result<SubjectSummaryDto>> Handle(
        RegisterSubjectCommand request,
        CancellationToken cancellationToken
    )
    {
        var repository = unitOfWork.Subjects;
        var subject = Subject.Register(
            UlidId.NewUlid(),
            request.GivenName,
            request.FamilyName,
            request.DateOfBirth,
            request.Email,
            request.RegisteredAt);

        await repository.AddAsync(subject, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var summary = await subjectQueries.GetSummaryByIdAsync(
            subject.Id.ToString(),
            cancellationToken);

        return summary is null
            ? Result.Fail<SubjectSummaryDto>("Failed to load created subject.")
            : Result.Success(summary);
    }
}