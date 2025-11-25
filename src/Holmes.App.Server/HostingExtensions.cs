using Holmes.App.Server.Endpoints;
using Holmes.App.Server.Middleware;
using Holmes.App.Server.Security;
using Holmes.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace Holmes.App.Server;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseHolmesSerilog();

        builder.Services
            .AddHolmesObservability(builder.Configuration)
            .AddHolmesWebStack()
            .AddHolmesAuthentication(builder.Configuration, builder.Environment)
            .AddHolmesAuthorization()
            .AddHolmesApplicationCore()
            .AddHolmesDataProtection(builder.Environment)
            .AddHolmesSwagger(builder.Environment)
            .AddHolmesModules(builder.Configuration, builder.Environment);

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        app.UseMiddleware<RedirectToAuthOptionsMiddleware>();

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseMiddleware<EnsureHolmesUserMiddleware>();
        app.UseAuthorization();
        app.MapControllers();

        ConfigureAuthEndpoints(app);
        ConfigureDiagnostics(app);

        return app;
    }

    private static void ConfigureAuthEndpoints(WebApplication app)
    {
        var auth = app.MapGroup("/auth");

        auth.MapGet("/options", (HttpRequest request, string? returnUrl) =>
            {
                var destination = ReturnUrlSanitizer.Sanitize(returnUrl, request);
                var html = AuthPageRenderer.RenderOptionsPage(destination);
                return Results.Content(html, "text/html");
            })
            .AllowAnonymous();

        auth.MapGet("/login", (HttpRequest request, string? returnUrl) =>
            {
                var destination = ReturnUrlSanitizer.Sanitize(returnUrl, request);
                return Results.Challenge(
                    new AuthenticationProperties { RedirectUri = destination },
                    [OpenIdConnectDefaults.AuthenticationScheme]);
            })
            .AllowAnonymous();

        auth.MapGet("/access-denied", (string? reason) =>
            {
                var html = AuthPageRenderer.RenderAccessDeniedPage(reason);
                return Results.Content(html, "text/html");
            })
            .AllowAnonymous();

        auth.MapPost("/logout", async (HttpContext context, string? returnUrl) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var target = ReturnUrlSanitizer.Sanitize(returnUrl, context.Request);
                return Results.Redirect(target);
            })
            .RequireAuthorization();
    }

    private static void ConfigureDiagnostics(WebApplication app)
    {
        app.MapHealthChecks("/health")
            .AllowAnonymous();

        app.MapPrometheusScrapingEndpoint()
            .AllowAnonymous();

        app.Map("/error", (HttpContext context, IHostEnvironment env, ILogger<Program> logger) =>
            {
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (exceptionFeature?.Error is not null)
                {
                    logger.LogError(exceptionFeature.Error, "Unhandled request error");
                }

                var detail = env.IsDevelopment() || env.IsEnvironment("Testing")
                    ? exceptionFeature?.Error?.ToString()
                    : null;

                return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: detail);
            })
            .AllowAnonymous();

        app.MapGet("/_info", (IHostEnvironment env) =>
                Results.Ok(new
                {
                    service = typeof(HostingExtensions).Assembly.GetName().Name,
                    version = typeof(HostingExtensions).Assembly.GetName().Version?.ToString(),
                    environment = env.EnvironmentName,
                    machine = Environment.MachineName
                }))
            .AllowAnonymous();
    }
}