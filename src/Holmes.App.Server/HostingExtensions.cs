using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.App.Server.Infrastructure;
using Holmes.App.Server.Security;
using Holmes.App.Server.Workflow;
using Holmes.Core.Application;
using Holmes.Core.Application.Behaviors;
using Holmes.Core.Domain.Security;
using Holmes.Core.Infrastructure.Security;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Intake.Application.Gateways;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Users.Application.Commands;
using Holmes.Users.Application.Exceptions;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Holmes.Users.Infrastructure.Sql.Repositories;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Holmes.App.Server;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
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

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddHealthChecks();
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                "Holmes.App.Server",
                serviceVersion: typeof(HostingExtensions).Assembly.GetName().Version?.ToString()))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(UnitOfWorkTelemetry.MeterName)
                    .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(UnitOfWorkTelemetry.ActivitySourceName);

                var otlpEndpoint = builder.Configuration["OpenTelemetry:Exporter:Endpoint"];
                tracing.AddOtlpExporter(options =>
                {
                    if (!string.IsNullOrWhiteSpace(otlpEndpoint) &&
                        Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                    {
                        options.Endpoint = endpoint;
                    }
                    // otherwise rely on OTEL_EXPORTER_OTLP_ENDPOINT env var (e.g., Rider)
                });
            });

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        ConfigureAuthentication(builder);
        builder.Services.AddScoped<IUserContext, HttpUserContext>();
        builder.Services.AddScoped<ICurrentUserInitializer, CurrentUserInitializer>();

        var isRunningInTestHost = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TESTHOST"), "1",
            StringComparison.Ordinal);
        var isTestEnvironment = builder.Environment.IsEnvironment("Testing");
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || isTestEnvironment || isRunningInTestHost)
        {
            builder.Services.AddInfrastructureForTesting();
        }
        else
        {
            builder.Services.AddInfrastructure(connectionString);
        }

        var temp1 = builder.Services;
        builder.Services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<RequestBase>();
            config.RegisterServicesFromAssemblyContaining<RegisterExternalUserCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterCustomerCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterSubjectCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterSubjectCommand>();
        });

        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AssignUserBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.RequireAdmin, policy => policy.RequireRole("Admin"))
            .AddPolicy(AuthorizationPolicies.RequireOps, policy => policy.RequireRole("Operations", "Admin"));

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddHostedService<DevelopmentDataSeeder>();
        }

        var dataProtection = builder.Services.AddDataProtection()
            .SetApplicationName("Holmes");
        if (isTestEnvironment || isRunningInTestHost)
        {
            dataProtection.UseEphemeralDataProtectionProvider();
        }
        else
        {
            dataProtection.PersistKeysToDbContext<CoreDbContext>();
        }

        if (builder.Environment.IsDevelopment())
        {
            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Holmes API"
                    });
                });
        }

        var retval = builder.Build();
        return retval;
    }

    private static void ConfigureAuthentication(WebApplicationBuilder builder)
    {
        var isRunningInTestHost = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TESTHOST"),
            "1",
            StringComparison.Ordinal);
        if (builder.Environment.IsEnvironment("Testing") || isRunningInTestHost)
        {
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthenticationDefaults.Scheme;
                    options.DefaultAuthenticateScheme = TestAuthenticationDefaults.Scheme;
                    options.DefaultChallengeScheme = TestAuthenticationDefaults.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationDefaults.Scheme, _ => { });
            return;
        }

        var authority = builder.Configuration["Authentication:Authority"];
        var clientId = builder.Configuration["Authentication:ClientId"];
        var clientSecret = builder.Configuration["Authentication:ClientSecret"];

        if (string.IsNullOrWhiteSpace(authority) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "Interactive authentication requires Authentication:Authority, ClientId, and ClientSecret.");
        }

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/auth/login";
                options.LogoutPath = "/auth/logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.Name = "holmes.auth";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.MapInboundClaims = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");
                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
                options.ClaimActions.MapJsonKey(ClaimTypes.Role, "role");
                options.ClaimActions.MapJsonKey("preferred_username", "preferred_username");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events ??= new OpenIdConnectEvents();
                options.Events.OnRedirectToIdentityProvider = context =>
                {
                    if (IsApiRequest(context.Request))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.HandleResponse();
                    }

                    return Task.CompletedTask;
                };
            });
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
        app.Use(async (context, next) =>
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false) &&
                ShouldRedirectToAuthOptions(context.Request))
            {
                var returnUrl1 = GetRequestedUrl(context.Request);
                context.Response.Redirect($"/auth/options?returnUrl={Uri.EscapeDataString(returnUrl1)}");
                return;
            }

            await next();
        });

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated == true && RequiresUserInitialization(context.Request))
            {
                var initializer = context.RequestServices.GetRequiredService<ICurrentUserInitializer>();
                try
                {
                    await initializer.EnsureCurrentUserIdAsync(context.RequestAborted);
                }
                catch (UserInvitationRequiredException ex)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "Uninvited login attempt for {Email} ({Issuer}/{Subject})", ex.Email,
                        ex.Issuer, ex.Subject);
                    if (!string.Equals(context.User.Identity?.AuthenticationType,
                            TestAuthenticationDefaults.Scheme, StringComparison.Ordinal))
                    {
                        await context.SignOutAsync();
                    }

                    context.Response.Redirect("/auth/access-denied?reason=uninvited");
                    return;
                }
            }

            await next();
        });
        app.UseAuthorization();

        app.MapControllers();

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

        return app;
    }

    private static bool IsApiRequest(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (request.Headers.TryGetValue("Accept", out var acceptHeader) &&
            acceptHeader.Any(value =>
                value is not null &&
                value.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
            requestedWith.Any(value =>
                value is not null &&
                value.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static bool RequiresUserInitialization(HttpRequest request)
    {
        var path = request.Path;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsPost(request.Method))
        {
            return true;
        }

        if (path.StartsWithSegments("/signin-oidc", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/signout-callback-oidc", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/access-denied", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/options", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool ShouldRedirectToAuthOptions(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        var path = request.Path;
        if (path.StartsWithSegments("/auth") ||
            path.StartsWithSegments("/signin-oidc") ||
            path.StartsWithSegments("/signout-callback-oidc") ||
            path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/health") ||
            path.StartsWithSegments("/swagger") ||
            path.StartsWithSegments("/static") ||
            path.StartsWithSegments("/assets"))
        {
            return false;
        }

        if (path.HasValue && path.Value.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        if (!request.Headers.TryGetValue("Accept", out var acceptHeader))
        {
            return false;
        }

        return acceptHeader.Any(value =>
            value is not null &&
            value.Contains("text/html", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRequestedUrl(HttpRequest request)
    {
        var path = request.Path.HasValue ? request.Path.Value : "/";
        var query = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
        return string.Concat(path, query);
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

    private static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString
    )
    {
        ServerVersion serverVersion;
        try
        {
            serverVersion = ServerVersion.AutoDetect(connectionString);
        }
        catch (MySqlException)
        {
            serverVersion = new MySqlServerVersion(new Version(8, 0, 34));
        }

        /* Core */
        services.AddCoreInfrastructureSql(connectionString, serverVersion);
        services.AddCoreInfrastructureSecurity();

        /* Users */
        services.AddUsersInfrastructureSql(connectionString, serverVersion);

        /* Customers */
        services.AddCustomersInfrastructureSql(connectionString, serverVersion);

        /* Subjects */
        services.AddSubjectsInfrastructureSql(connectionString, serverVersion);

        /* Intake */
        services.AddIntakeInfrastructureSql(connectionString, serverVersion);
        services.AddScoped<IOrderWorkflowGateway, OrderWorkflowGateway>();

        /* Workflow */
        services.AddWorkflowInfrastructureSql(connectionString, serverVersion);

        return services;
    }

    private static IServiceCollection AddInfrastructureForTesting(this IServiceCollection services)
    {
        services.AddDbContext<CoreDbContext>(options => options.UseInMemoryDatabase("holmes-core"));
        services.AddDbContext<UsersDbContext>(options => options.UseInMemoryDatabase("holmes-users"));
        services.AddDbContext<CustomersDbContext>(options => options.UseInMemoryDatabase("holmes-customers"));
        services.AddDbContext<SubjectsDbContext>(options => options.UseInMemoryDatabase("holmes-subjects"));
        services.AddDbContext<IntakeDbContext>(options => options.UseInMemoryDatabase("holmes-intake"));
        services.AddDbContext<WorkflowDbContext>(options => options.UseInMemoryDatabase("holmes-workflow"));
        services.AddSingleton<IAeadEncryptor, NoOpAeadEncryptor>();
        services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();
        services.AddScoped<IUserDirectory, SqlUserDirectory>();
        services.AddScoped<ICustomersUnitOfWork, CustomersUnitOfWork>();
        services.AddScoped<ISubjectsUnitOfWork, SubjectsUnitOfWork>();
        services.AddScoped<IIntakeUnitOfWork, IntakeUnitOfWork>();
        services.AddScoped<IWorkflowUnitOfWork, WorkflowUnitOfWork>();
        services.AddScoped<IOrderWorkflowGateway, OrderWorkflowGateway>();
        return services;
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
