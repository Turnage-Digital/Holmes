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
    private readonly ISubjectRepository _repository;
    private readonly ISubjectsUnitOfWork _unitOfWork;

    public RegisterSubjectCommandHandler(
        ISubjectRepository repository,
        ISubjectsUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UlidId> Handle(RegisterSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = Subject.Register(
            UlidId.NewUlid(),
            request.GivenName,
            request.FamilyName,
            request.DateOfBirth,
            request.Email,
            request.RegisteredAt);

        await _repository.AddAsync(subject, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return subject.Id;
    }
}