using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.App.Server.Security;

public sealed class CurrentUserInitializer(IUserContext userContext) : ICurrentUserInitializer
{
    public Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = userContext.Principal.FindFirst("holmes_user_id")?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            throw new InvalidOperationException("Required claim 'holmes_user_id' was not present.");
        }

        return Task.FromResult(UlidId.Parse(userIdClaim));
    }
}