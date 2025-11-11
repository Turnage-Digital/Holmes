using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed record RegisterSubjectCommand(
    string GivenName,
    string FamilyName,
    DateOnly? DateOfBirth,
    string? Email,
    DateTimeOffset RegisteredAt
) : RequestBase<UlidId>;

public sealed class RegisterSubjectCommandHandler : IRequestHandler<RegisterSubjectCommand, UlidId>
{
    private readonly ISubjectsUnitOfWork _unitOfWork;

    public RegisterSubjectCommandHandler(
        ISubjectsUnitOfWork unitOfWork
    )
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UlidId> Handle(RegisterSubjectCommand request, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.Subjects;
        var subject = Subject.Register(
            UlidId.NewUlid(),
            request.GivenName,
            request.FamilyName,
            request.DateOfBirth,
            request.Email,
            request.RegisteredAt);

        await repository.AddAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return subject.Id;
    }
}