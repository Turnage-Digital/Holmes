using Holmes.App.Server.Middleware;
using Microsoft.AspNetCore.Http;

namespace Holmes.App.Server.Tests.Middleware;

[TestFixture]
public class RedirectToAuthOptionsMiddlewareTests
{
    [Test]
    public async Task Redirects_Html_Navigation_When_User_Not_Authenticated()
    {
        var middleware = new RedirectToAuthOptionsMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/users";
        context.Request.Headers.Accept = "text/html";

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(nextCalled, Is.False);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status302Found));
        Assert.That(context.Response.Headers.Location.ToString(), Is.EqualTo("/auth/options?returnUrl=%2Fusers"));
    }

    [Test]
    public async Task Skips_Redirect_For_Api_Request()
    {
        var middleware = new RedirectToAuthOptionsMiddleware();
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/orders";
        context.Request.Headers.Accept = "application/json";

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.That(nextCalled, Is.True);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
        Assert.That(context.Response.Headers.Location.Count, Is.EqualTo(0));
    }
}