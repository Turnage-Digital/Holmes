using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Exceptions;
using Holmes.Users.Domain;
using Holmes.Users.Domain.Events;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record RegisterExternalUserCommand(
    string Issuer,
    string Subject,
    string Email,
    string? DisplayName,
    string? AuthenticationMethod,
    DateTimeOffset AuthenticatedAt,
    bool AllowAutoProvision = false
) : RequestBase<UlidId>, ISkipUserAssignment;

public sealed class RegisterExternalUserCommandHandler(IUsersUnitOfWork unitOfWork, IPublisher publisher)
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
                if (!request.AllowAutoProvision)
                {
                    await publisher.Publish(new UninvitedExternalLoginAttempted(
                        request.Issuer,
                        request.Subject,
                        request.Email,
                        request.AuthenticatedAt), cancellationToken);
                    throw new UserInvitationRequiredException(request.Email, request.Issuer, request.Subject);
                }

                var user = User.Register(UlidId.NewUlid(), identity, request.Email, request.DisplayName,
                    request.AuthenticatedAt);
                await repository.AddAsync(user, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return user.Id;
            }

            existing.LinkExternalIdentity(identity);
        }

        existing.MarkIdentitySeen(request.Issuer, request.Subject, request.AuthenticatedAt);
        existing.UpdateProfile(request.Email, request.DisplayName, request.AuthenticatedAt);
        if (existing.Status == UserStatus.Invited)
        {
            existing.ActivatePendingInvitation(request.AuthenticatedAt);
        }

        await repository.UpdateAsync(existing, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return existing.Id;
    }
}