using System.Security.Claims;
using Holmes.App.Server.Middleware;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holmes.App.Server.Tests.Middleware;

[TestFixture]
public class EnsureHolmesUserMiddlewareTests
{
    [Test]
    public async Task Authenticated_Request_Invokes_Initializer_And_Calls_Next()
    {
        var initializer = new CapturingInitializer();
        var middleware = new EnsureHolmesUserMiddleware(initializer, NullLogger<EnsureHolmesUserMiddleware>.Instance);
        var context = CreateAuthenticatedContext("/dashboard");

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(initializer.InvocationCount, Is.EqualTo(1));
        Assert.That(nextCalled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
    }

    [Test]
    public async Task Excluded_Paths_Do_Not_Invoke_Initializer()
    {
        var initializer = new CapturingInitializer();
        var middleware = new EnsureHolmesUserMiddleware(initializer, NullLogger<EnsureHolmesUserMiddleware>.Instance);
        var context = CreateAuthenticatedContext("/auth/login");

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.That(initializer.InvocationCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Uninvited_User_Is_Signed_Out_And_Redirected()
    {
        var initializer = new ThrowingInitializer();
        var middleware = new EnsureHolmesUserMiddleware(initializer, NullLogger<EnsureHolmesUserMiddleware>.Instance);
        var context = CreateAuthenticatedContext("/api/users/me");
        var authService = (StubAuthenticationService)context.RequestServices.GetRequiredService<IAuthenticationService>();

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(nextCalled, Is.False);
        Assert.That(authService.SignOutCalls, Is.EqualTo(1));
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status302Found));
        Assert.That(context.Response.Headers.Location.ToString(), Is.EqualTo("/auth/access-denied?reason=uninvited"));
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string path)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService, StubAuthenticationService>();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };

        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity("Cookies") { });

        return context;
    }

    private sealed class CapturingInitializer : ICurrentUserInitializer
    {
        public int InvocationCount { get; private set; }

        public Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(UlidId.NewUlid());
        }
    }

    private sealed class ThrowingInitializer : ICurrentUserInitializer
    {
        public Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken)
        {
            throw new UserInvitationRequiredException("user@holmes.dev", "issuer", "subject");
        }
    }

    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public int SignOutCalls { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignOutCalls++;
            return Task.CompletedTask;
        }
    }
}
