using Holmes.Users.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Users.Application.EventHandlers;

public sealed class UninvitedExternalLoginAttemptedHandler(ILogger<UninvitedExternalLoginAttemptedHandler> logger)
    : INotificationHandler<UninvitedExternalLoginAttempted>
{
    public Task Handle(UninvitedExternalLoginAttempted notification, CancellationToken cancellationToken)
    {
        logger.LogWarning("Uninvited login attempt recorded for {Email} ({Issuer}/{Subject}) at {Timestamp}",
            notification.Email, notification.Issuer, notification.Subject, notification.AttemptedAt);
        return Task.CompletedTask;
    }
}