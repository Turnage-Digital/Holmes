using Holmes.Users.Application.Abstractions;
using Holmes.Users.Domain;
using Holmes.Users.Domain.Events;
using MediatR;

namespace Holmes.Users.Application.EventHandlers;

/// <summary>
///     Handles user domain events to maintain the user projection table.
///     This replaces the synchronous UpsertDirectory calls in the repository.
/// </summary>
public sealed class UserProjectionHandler(IUserProjectionWriter writer)
    : INotificationHandler<UserInvited>,
        INotificationHandler<UserRegistered>,
        INotificationHandler<UserProfileUpdated>,
        INotificationHandler<UserSuspended>,
        INotificationHandler<UserReactivated>
{
    public Task Handle(UserInvited notification, CancellationToken cancellationToken)
    {
        var model = new UserProjectionModel(
            notification.UserId.ToString(),
            notification.Email,
            notification.DisplayName,
            "urn:holmes:invite",
            notification.UserId.ToString(),
            notification.InvitedAt,
            UserStatus.Invited);

        return writer.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(UserProfileUpdated notification, CancellationToken cancellationToken)
    {
        return writer.UpdateProfileAsync(
            notification.UserId.ToString(),
            notification.Email,
            notification.DisplayName,
            cancellationToken);
    }

    public Task Handle(UserReactivated notification, CancellationToken cancellationToken)
    {
        return writer.UpdateStatusAsync(
            notification.UserId.ToString(),
            UserStatus.Active,
            cancellationToken);
    }

    public Task Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        var model = new UserProjectionModel(
            notification.UserId.ToString(),
            notification.Email,
            notification.DisplayName,
            notification.Issuer,
            notification.Subject,
            notification.RegisteredAt,
            UserStatus.Active);

        return writer.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(UserSuspended notification, CancellationToken cancellationToken)
    {
        return writer.UpdateStatusAsync(
            notification.UserId.ToString(),
            UserStatus.Suspended,
            cancellationToken);
    }
}