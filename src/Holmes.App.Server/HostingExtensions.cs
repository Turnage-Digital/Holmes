using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.App.Server.Security;
using Holmes.Core.Application;
using Holmes.Core.Application.Behaviors;
using Holmes.Core.Domain.Security;
using Holmes.Core.Infrastructure.Security;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Repositories;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Repositories;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Holmes.Users.Infrastructure.Sql.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MySqlConnector;
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

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        ConfigureAuthentication(builder);
        builder.Services.AddScoped<IUserContext, HttpUserContext>();

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

        builder.Services.AddDomain();
        builder.Services.AddApplication();

        builder.Services.AddAuthorization();

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

        var googleSection = builder.Configuration.GetSection("Authentication:Google");
        var googleClientId = googleSection["ClientId"];
        var googleClientSecret = googleSection["ClientSecret"];
        if (string.IsNullOrWhiteSpace(googleClientId) ||
            string.IsNullOrWhiteSpace(googleClientSecret))
        {
            throw new InvalidOperationException(
                "Google authentication is required. Configure Authentication:Google:ClientId and ClientSecret.");
        }

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/auth/login";
                options.LogoutPath = "/auth/logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            })
            .AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.SaveTokens = true;
                options.ClaimActions.MapJsonKey("picture", "picture");
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
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
            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                if (ShouldRedirectToAuthOptions(context.Request))
                {
                    var returnUrl = GetRequestedUrl(context.Request);
                    context.Response.Redirect($"/auth/options?returnUrl={Uri.EscapeDataString(returnUrl)}");
                    return;
                }
            }

            await next();
        });

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        var auth = app.MapGroup("/auth");

        auth.MapGet("/options", (HttpRequest request, string? returnUrl) =>
            {
                var destination = SanitizeReturnUrl(returnUrl, request);
                var html = $@"<!DOCTYPE html>
<html lang=""en"">
  <head>
    <meta charset=""utf-8"" />
    <title>Holmes Sign In</title>
    <style>
      :root {{
        font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", sans-serif;
      }}
      body {{
        margin: 0;
        background: #f5f5f5;
        min-height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
      }}
      .card {{
        background: #fff;
        padding: 2.5rem;
        border-radius: 16px;
        width: min(400px, 90vw);
        text-align: center;
        box-shadow: 0 25px 80px rgba(0,0,0,0.12);
      }}
      h1 {{ margin-top: 0; color: #1b2e5f; }}
      p {{ color: #555; }}
      .btn {{
        display: inline-flex;
        justify-content: center;
        align-items: center;
        padding: 0.85rem 1.2rem;
        background: #1b2e5f;
        color: #fff;
        text-decoration: none;
        border-radius: 6px;
        font-weight: 600;
      }}
      .btn:hover {{ background: #16244a; }}
    </style>
  </head>
  <body>
    <div class=""card"">
      <h1>Sign in to Holmes</h1>
      <p>Select an identity provider to continue.</p>
      <a class=""btn"" href=""/auth/login?returnUrl={Uri.EscapeDataString(destination)}"">Continue with Google</a>
    </div>
  </body>
</html>";
                return Results.Content(html, "text/html");
            })
            .AllowAnonymous();

        auth.MapGet("/login", (HttpRequest request, string? returnUrl) =>
            {
                var destination = SanitizeReturnUrl(returnUrl, request);
                return Results.Challenge(
                    new AuthenticationProperties { RedirectUri = destination },
                    [GoogleDefaults.AuthenticationScheme]);
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

    private static bool ShouldRedirectToAuthOptions(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        var path = request.Path;
        if (path.StartsWithSegments("/auth") ||
            path.StartsWithSegments("/signin-google") ||
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
            if (!string.Equals(
                    absolute.Host,
                    requestHost,
                    StringComparison.OrdinalIgnoreCase))
            {
                return "/";
            }

            return absolute.PathAndQuery;
        }

        return returnUrl.StartsWith("/", StringComparison.Ordinal) ? returnUrl : "/";
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

        return services;
    }

    private static IServiceCollection AddInfrastructureForTesting(this IServiceCollection services)
    {
        services.AddDbContext<CoreDbContext>(options => options.UseInMemoryDatabase("holmes-core"));
        services.AddDbContext<UsersDbContext>(options => options.UseInMemoryDatabase("holmes-users"));
        services.AddDbContext<CustomersDbContext>(options => options.UseInMemoryDatabase("holmes-customers"));
        services.AddDbContext<SubjectsDbContext>(options => options.UseInMemoryDatabase("holmes-subjects"));
        services.AddSingleton<IAeadEncryptor, NoOpAeadEncryptor>();
        services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();
        services.AddScoped<IUserRepository>(sp => sp.GetRequiredService<IUsersUnitOfWork>().Users);
        services.AddScoped<IUserDirectory>(sp => sp.GetRequiredService<IUsersUnitOfWork>().UserDirectory);
        services.AddScoped<ICustomersUnitOfWork, CustomersUnitOfWork>();
        services.AddScoped<ICustomerRepository>(sp => sp.GetRequiredService<ICustomersUnitOfWork>().Customers);
        services.AddScoped<ISubjectsUnitOfWork, SubjectsUnitOfWork>();
        services.AddScoped<ISubjectRepository>(sp => sp.GetRequiredService<ISubjectsUnitOfWork>().Subjects);
        return services;
    }

    private static IServiceCollection AddDomain(this IServiceCollection services)
    {
        return services;
    }

    private static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<RequestBase>();
            config.RegisterServicesFromAssemblyContaining<RegisterExternalUserCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterCustomerCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterSubjectCommand>();
        });

        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(LoggingBehavior<,>));
        return services;
    }
}
