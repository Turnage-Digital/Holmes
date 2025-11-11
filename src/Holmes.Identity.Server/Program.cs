using System.Security.Claims;
using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Holmes.Identity.Server;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level} {SourceContext}]{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, config) =>
    {
        var seqUrl = ctx.Configuration["Seq:Url"];
        var seqApiKey = ctx.Configuration["Seq:ApiKey"];

        config.WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level} {SourceContext}]{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
            .Enrich.WithCorrelationIdHeader("X-Correlation-ID")
            .Enrich.FromLogContext();

        if (!string.IsNullOrWhiteSpace(seqUrl))
        {
            if (!string.IsNullOrWhiteSpace(seqApiKey))
            {
                config.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
            }
            else
            {
                config.WriteTo.Seq(seqUrl);
            }
        }
    });

    builder.Services
        .AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.UserInteraction.LoginUrl = "/dev/login";
            options.UserInteraction.LogoutUrl = "/dev/logout";
        })
        .AddInMemoryIdentityResources(Config.IdentityResources)
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryClients(Config.Clients)
        .AddProfileService<ProfileService>();

    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSerilogRequestLogging();

    app.UseIdentityServer();
    app.UseAuthorization();

    app.MapGet("/", () => Results.Redirect("/.well-known/openid-configuration"));

    app.MapGet("/dev/login", (
            HttpContext context,
            string? returnUrl,
            IIdentityServerInteractionService interaction
        ) =>
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                !string.IsNullOrEmpty(returnUrl) &&
                interaction.IsValidReturnUrl(returnUrl))
            {
                return Results.Redirect(returnUrl);
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && !interaction.IsValidReturnUrl(returnUrl))
            {
                return Results.BadRequest("Invalid returnUrl");
            }

            var error = context.Request.Query["error"] == "1";
            var message = error ? "<p style=\"color:red\">Invalid credentials</p>" : string.Empty;
            var encodedReturnUrl = returnUrl ?? "/";

            var html = $$"""
                         <!DOCTYPE html>
                             <html lang="en">
                               <head>
                                 <meta charset="utf-8" />
                                 <title>Holmes Dev Login</title>
                                 <style>
                                   body {
                                     font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
                                     background: #f4f6fb;
                                     display: flex;
                                     align-items: center;
                                     justify-content: center;
                                     min-height: 100vh;
                                     margin: 0;
                                   }
                                   .card {
                                     background: white;
                                     padding: 2rem;
                                     border-radius: 12px;
                                     box-shadow: 0 20px 60px rgba(0,0,0,0.12);
                                     width: 320px;
                                   }
                                   label { display:block; margin-top: 1rem; font-weight: 600; }
                                   input {
                                     width: 100%;
                                     padding: 0.65rem;
                                     border: 1px solid #c8d0e0;
                                     border-radius: 6px;
                                     margin-top: 0.3rem;
                                   }
                                   button {
                                     width: 100%;
                                     margin-top: 1.5rem;
                                     background: #1b2e5f;
                                     color: white;
                                     border: none;
                                     padding: 0.75rem;
                                     border-radius: 6px;
                                     font-weight: 600;
                                     cursor: pointer;
                                   }
                                   button:hover { background: #142046; }
                                 </style>
                               </head>
                               <body>
                                 <div class="card">
                                   <h2>Holmes Dev Login</h2>
                                   {{message}}
                                   <form method="post" action="/dev/login">
                                     <input type="hidden" name="returnUrl" value="{{encodedReturnUrl}}" />
                                     <label for="username">Username</label>
                                     <input id="username" name="username" autofocus />
                                     <label for="password">Password</label>
                                     <input id="password" name="password" type="password" />
                                     <button type="submit">Sign in</button>
                                   </form>
                                 </div>
                               </body>
                             </html>
                         """;

            return Results.Content(html, "text/html");
        })
        .AllowAnonymous();

    app.MapPost("/dev/login", async (
            HttpContext context,
            IIdentityServerInteractionService interaction
        ) =>
        {
            var form = await context.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            var user = Config.DevUsers.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(u.Password, password, StringComparison.Ordinal));

            if (user is not null)
            {
                var idsUser = new IdentityServerUser(user.SubjectId)
                {
                    DisplayName = user.DisplayName,
                    AdditionalClaims = new List<Claim>
                    {
                        new(JwtClaimTypes.Name, user.DisplayName),
                        new(JwtClaimTypes.Email, user.Email),
                        new(JwtClaimTypes.PreferredUserName, user.Username),
                        new(JwtClaimTypes.Role, user.Role)
                    }
                };

                await context.SignInAsync(idsUser.CreatePrincipal());

                if (!string.IsNullOrWhiteSpace(returnUrl) && interaction.IsValidReturnUrl(returnUrl))
                {
                    return Results.Redirect(returnUrl);
                }

                return Results.Redirect("/");
            }

            var redirectUrl = $"/dev/login?returnUrl={Uri.EscapeDataString(returnUrl)}&error=1";
            return Results.Redirect(redirectUrl);
        })
        .AllowAnonymous();

    app.MapPost("/dev/logout", async (
            HttpContext context,
            string? logoutId,
            IIdentityServerInteractionService interaction
        ) =>
        {
            await context.SignOutAsync();

            if (!string.IsNullOrEmpty(logoutId))
            {
                var logout = await interaction.GetLogoutContextAsync(logoutId);
                if (!string.IsNullOrEmpty(logout?.PostLogoutRedirectUri))
                {
                    return Results.Redirect(logout.PostLogoutRedirectUri);
                }
            }

            return Results.Redirect("/");
        })
        .AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not "HostAbortedException")
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}

// public partial class Program;