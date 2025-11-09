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
) : RequestBase<UlidId>, ISkipUserAssignment;

public sealed class RegisterExternalUserCommandHandler(IUsersUnitOfWork unitOfWork)
    : IRequestHandler<RegisterExternalUserCommand, UlidId>
{
    public async Task<UlidId> Handle(RegisterExternalUserCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Users;
        var identity = new ExternalIdentity(request.Issuer, request.Subject, request.AuthenticationMethod,
            request.AuthenticatedAt);
        var existing = await repository.GetByExternalIdentityAsync(request.Issuer, request.Subject, cancellationToken);
        if (existing is null)
        {
            existing = await repository.GetByEmailAsync(request.Email, cancellationToken);
            if (existing is null)
            {
                var user = User.Register(UlidId.NewUlid(), identity, request.Email, request.DisplayName,
                    request.AuthenticatedAt);
                await repository.AddAsync(user, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return user.Id;
            }

            existing.LinkExternalIdentity(identity);
            existing.ActivatePendingInvitation(request.AuthenticatedAt);
            existing.MarkIdentitySeen(request.Issuer, request.Subject, request.AuthenticatedAt);
            existing.UpdateProfile(request.Email, request.DisplayName, request.AuthenticatedAt);
            await repository.UpdateAsync(existing, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        existing.MarkIdentitySeen(request.Issuer, request.Subject, request.AuthenticatedAt);
        existing.UpdateProfile(request.Email, request.DisplayName, request.AuthenticatedAt);
        await repository.UpdateAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return existing.Id;
    }
}
