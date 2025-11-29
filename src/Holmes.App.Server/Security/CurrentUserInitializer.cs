using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Commands;
using MediatR;

namespace Holmes.App.Server.Security;

public sealed class CurrentUserInitializer(
    IUserContext userContext,
    IMediator mediator
) : ICurrentUserInitializer
{
    public async Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = userContext.Principal.FindFirst("holmes_user_id")?.Value;
        if (!string.IsNullOrWhiteSpace(userIdClaim) && Ulid.TryParse(userIdClaim, out var parsed))
        {
            return UlidId.FromUlid(parsed);
        }

        var command = new RegisterExternalUserCommand(
            userContext.Issuer,
            userContext.Subject,
            userContext.Email,
            userContext.DisplayName,
            userContext.AuthenticationMethod,
            DateTimeOffset.UtcNow);

        return await mediator.Send(command, cancellationToken);
    }
}