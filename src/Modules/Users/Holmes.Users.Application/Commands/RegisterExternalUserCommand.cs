using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Exceptions;
using Holmes.Users.Contracts;
using Holmes.Users.Domain;
using Holmes.Users.Domain.Events;
using Holmes.Users.Domain.ValueObjects;
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
) : RequestBase<UlidId>;

public sealed class RegisterExternalUserCommandHandler(
    IUsersUnitOfWork unitOfWork,
    IUserQueries userQueries,
    IPublisher publisher
)
    : IRequestHandler<RegisterExternalUserCommand, UlidId>
{
    public async Task<UlidId> Handle(RegisterExternalUserCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Users;
        var identity = new ExternalIdentity(request.Issuer, request.Subject, request.AuthenticationMethod,
            request.AuthenticatedAt);

        // Check if user exists by external identity (query side)
        var existingByIdentity = await userQueries.GetByExternalIdentityAsync(
            request.Issuer, request.Subject, cancellationToken);

        if (existingByIdentity is null)
        {
            // Check by email (query side)
            var existingByEmail = await userQueries.GetByEmailAsync(request.Email, cancellationToken);

            if (existingByEmail is null)
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

            // Re-fetch for mutation via repository (command side)
            var existingUser = await repository.GetByIdAsync(
                UlidId.Parse(existingByEmail.Id), cancellationToken);
            if (existingUser is null)
            {
                throw new InvalidOperationException($"User '{existingByEmail.Id}' not found after query.");
            }

            existingUser.LinkExternalIdentity(identity);
            existingUser.MarkIdentitySeen(request.Issuer, request.Subject, request.AuthenticatedAt);
            existingUser.UpdateProfile(request.Email, request.DisplayName, request.AuthenticatedAt);
            if (existingUser.Status == UserStatus.Invited)
            {
                existingUser.ActivatePendingInvitation(request.AuthenticatedAt);
            }

            await repository.UpdateAsync(existingUser, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return existingUser.Id;
        }

        // Re-fetch for mutation via repository (command side)
        var existing = await repository.GetByIdAsync(
            UlidId.Parse(existingByIdentity.Id), cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"User '{existingByIdentity.Id}' not found after query.");
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
