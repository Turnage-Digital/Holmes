using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Commands;
using MediatR;

namespace Holmes.App.Server.Security;

public sealed class CurrentUserInitializer(
    IMediator mediator,
    IUserContext userContext
) : ICurrentUserInitializer
{
    private UlidId? _cachedUserId;

    public async Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        if (_cachedUserId is not null)
        {
            return _cachedUserId.Value;
        }

        var command = new RegisterExternalUserCommand(
            userContext.Issuer,
            userContext.Subject,
            userContext.Email,
            userContext.DisplayName,
            userContext.AuthenticationMethod,
            DateTimeOffset.UtcNow);

        _cachedUserId = await mediator.Send(command, cancellationToken);
        return _cachedUserId.Value;
    }
}