using System;
using Holmes.App.Server.DependencyInjection;
using Holmes.App.Server.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            app.UseHsts();
        }

        app.UseHttpsRedirection();
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
                var destination = SanitizeReturnUrl(returnUrl, request);
                var html = BuildAuthOptionsPage(destination);
                return Results.Content(html, "text/html");
            })
            .AllowAnonymous();

        auth.MapGet("/login", (HttpRequest request, string? returnUrl) =>
            {
                var destination = SanitizeReturnUrl(returnUrl, request);
                return Results.Challenge(
                    new AuthenticationProperties { RedirectUri = destination },
                    [OpenIdConnectDefaults.AuthenticationScheme]);
            })
            .AllowAnonymous();

        auth.MapGet("/access-denied", (string? reason) =>
            {
                var html = BuildAccessDeniedPage(reason);
                return Results.Content(html, "text/html");
            })
            .AllowAnonymous();

        auth.MapPost("/logout", async (HttpContext context, string? returnUrl) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var target = SanitizeReturnUrl(returnUrl, context.Request);
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

    private static string SanitizeReturnUrl(string? returnUrl, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute))
        {
            var requestHost = request.Host.HasValue ? request.Host.Host : string.Empty;
            if (!string.Equals(absolute.Host, requestHost, StringComparison.OrdinalIgnoreCase))
            {
                return "/";
            }

            return absolute.PathAndQuery;
        }

        return returnUrl.StartsWith('/') ? returnUrl : "/";
    }

    private static string BuildAuthOptionsPage(string destination)
    {
        var encoded = Uri.EscapeDataString(destination);
        return $$"""
                 <!DOCTYPE html>
                 <html lang="en">
                   <head>
                     <meta charset="utf-8" />
                     <title>Holmes Sign In</title>
                     <style>
                       :root {
                         font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                       }
                       body {
                         margin: 0;
                         background: #f5f5f5;
                         min-height: 100vh;
                         display: flex;
                         align-items: center;
                         justify-content: center;
                       }
                       .card {
                         background: #fff;
                         padding: 2.5rem;
                         border-radius: 16px;
                         width: min(400px, 90vw);
                         text-align: center;
                         box-shadow: 0 25px 80px rgba(0,0,0,0.12);
                       }
                       h1 { margin-top: 0; color: #1b2e5f; }
                       p { color: #555; }
                       .btn {
                         display: inline-flex;
                         justify-content: center;
                         align-items: center;
                         padding: 0.85rem 1.2rem;
                         background: #1b2e5f;
                         color: #fff;
                         text-decoration: none;
                         border-radius: 6px;
                         font-weight: 600;
                       }
                       .btn:hover { background: #16244a; }
                     </style>
                   </head>
                   <body>
                     <div class="card">
                       <h1>Sign in to Holmes</h1>
                       <p>Select an identity provider to continue.</p>
                       <a class="btn" href="/auth/login?returnUrl={{encoded}}">Continue with Holmes Identity</a>
                     </div>
                   </body>
                 </html>
                 """;
    }

    private static string BuildAccessDeniedPage(string? reason)
    {
        var (title, message) = reason switch
        {
            "uninvited" => ("Invitation Required",
                "You must be invited to Holmes before you can sign in. Please contact your administrator."),
            "suspended" => ("Account Suspended",
                "Your Holmes account has been suspended. Reach out to your administrator for assistance."),
            _ => ("Access Denied",
                "We could not grant you access to Holmes. Please verify your invitation or contact support.")
        };

        return $$"""
                 <!DOCTYPE html>
                 <html lang="en">
                   <head>
                     <meta charset="utf-8" />
                     <title>{{title}}</title>
                     <style>
                       :root {
                         font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                       }
                       body {
                         margin: 0;
                         background: #f5f5f5;
                         min-height: 100vh;
                         display: flex;
                         align-items: center;
                         justify-content: center;
                         color: #1b2e5f;
                       }
                       .card {
                         background: #fff;
                         padding: 2.5rem;
                         border-radius: 16px;
                         width: min(420px, 90vw);
                         text-align: center;
                         box-shadow: 0 25px 80px rgba(0,0,0,0.12);
                       }
                       h1 { margin-top: 0; }
                       p { color: #555; line-height: 1.5; }
                       a {
                         display: inline-block;
                         margin-top: 2rem;
                         color: #1b2e5f;
                         text-decoration: none;
                         font-weight: 600;
                       }
                     </style>
                   </head>
                   <body>
                     <div class="card">
                       <h1>{{title}}</h1>
                       <p>{{message}}</p>
                       <a href="/auth/options">Return to sign in</a>
                     </div>
                   </body>
                 </html>
                 """;
    }
}
