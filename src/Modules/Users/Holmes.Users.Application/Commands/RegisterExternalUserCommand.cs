using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record RegisterExternalUserCommand(
    string Issuer,
    string Subject,
    string Email,
    string? DisplayName,
    string? AuthenticationMethod,
    DateTimeOffset AuthenticatedAt
) : RequestBase<UlidId>;

public sealed class RegisterExternalUserCommandHandler : IRequestHandler<RegisterExternalUserCommand, UlidId>
{
    private readonly IUserRepository _repository;
    private readonly IUsersUnitOfWork _unitOfWork;

    public RegisterExternalUserCommandHandler(IUserRepository repository, IUsersUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UlidId> Handle(RegisterExternalUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByExternalIdentityAsync(request.Issuer, request.Subject, cancellationToken);
        if (existing is null)
        {
            var identity = new ExternalIdentity(request.Issuer, request.Subject, request.AuthenticationMethod,
                request.AuthenticatedAt);
            var user = User.Register(UlidId.NewUlid(), identity, request.Email, request.DisplayName,
                request.AuthenticatedAt);
            await _repository.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return user.Id;
        }

        existing.MarkIdentitySeen(request.Issuer, request.Subject, request.AuthenticatedAt);
        existing.UpdateProfile(request.Email, request.DisplayName, request.AuthenticatedAt);
        await _repository.UpdateAsync(existing, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return existing.Id;
    }
}