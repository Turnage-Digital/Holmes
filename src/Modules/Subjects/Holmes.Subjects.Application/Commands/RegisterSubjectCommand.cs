using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Application.Abstractions.Commands;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Application.Commands;

public sealed class RegisterSubjectCommandHandler(ISubjectsUnitOfWork unitOfWork)
    : IRequestHandler<RegisterSubjectCommand, UlidId>
{
    public async Task<UlidId> Handle(RegisterSubjectCommand request, CancellationToken cancellationToken)
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
        return subject.Id;
    }
}
