using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Commands;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;

namespace Holmes.App.Infrastructure.Security;

public sealed class CurrentUserInitializer(
    IUserContext userContext,
    IMediator mediator,
    IHostEnvironment hostEnvironment,
    IMemoryCache cache
) : ICurrentUserInitializer
{
    private const string CachePrefix = "current-user-id:";

    public async Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        var userIdClaim = userContext.Principal.FindFirst("holmes_user_id")?.Value;
        if (!string.IsNullOrWhiteSpace(userIdClaim) && Ulid.TryParse(userIdClaim, out var parsed))
        {
            return UlidId.FromUlid(parsed);
        }

        var cacheKey = $"{CachePrefix}{userContext.Issuer}|{userContext.Subject}";
        if (cache.TryGetValue(cacheKey, out UlidId cached))
        {
            return cached;
        }

        var command = new RegisterExternalUserCommand(
            userContext.Issuer,
            userContext.Subject,
            userContext.Email,
            userContext.DisplayName,
            userContext.AuthenticationMethod,
            DateTimeOffset.UtcNow,
            IsTestEnvironment());
        command.UserId = SystemActors.System;

        var userId = await mediator.Send(command, cancellationToken);
        cache.Set(cacheKey, userId, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(4)
        });

        return userId;
    }

    private bool IsTestEnvironment()
    {
        return hostEnvironment.IsEnvironment("Testing") ||
               string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TESTHOST"), "1",
                   StringComparison.Ordinal);
    }
}
