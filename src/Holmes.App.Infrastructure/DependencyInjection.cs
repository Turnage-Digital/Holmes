using System.IdentityModel.Tokens.Jwt;
using Holmes.App.Infrastructure.Identity;
using Holmes.App.Infrastructure.Security;
using Holmes.Core.Application;
using Holmes.Core.Domain.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Holmes.App.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        services.AddIdentityProvisioning();
        services.AddHolmesAuthentication(configuration, environment);
        services.AddHolmesAuthorization();
        services.AddHolmesSecurity();

        return services;
    }

    private static IServiceCollection AddIdentityProvisioning(this IServiceCollection services)
    {
        services.AddOptions<IdentityProvisioningOptions>()
            .BindConfiguration(IdentityProvisioningOptions.SectionName);
        services.AddHttpClient<IIdentityProvisioningClient, IdentityProvisioningClient>();

        return services;
    }

    private static IServiceCollection AddHolmesAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var isRunningInTestHost = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TESTHOST"),
            "1",
            StringComparison.Ordinal);

        if (environment.IsEnvironment("Testing") || isRunningInTestHost)
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthenticationDefaults.Scheme;
                    options.DefaultAuthenticateScheme = TestAuthenticationDefaults.Scheme;
                    options.DefaultChallengeScheme = TestAuthenticationDefaults.Scheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationDefaults.Scheme, _ => { });

            return services;
        }

        var authority = configuration["Authentication:Authority"];
        var clientId = configuration["Authentication:ClientId"];
        var clientSecret = configuration["Authentication:ClientSecret"];

        if (string.IsNullOrWhiteSpace(authority) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "Interactive authentication requires Authentication:Authority, ClientId, and ClientSecret.");
        }

        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

        services
            .AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = authority;
                options.Audience = "holmes.api";
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ValidateAudience = true
                };
            });

        return services;
    }

    private static IServiceCollection AddHolmesAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.RequireAdmin, policy => policy.RequireRole("Admin"))
            .AddPolicy(AuthorizationPolicies.RequireOps, policy => policy.RequireRole("Operations", "Admin"))
            .AddPolicy(AuthorizationPolicies.RequireGlobalAdmin,
                policy => policy.Requirements.Add(new GlobalAdminRequirement()));

        return services;
    }

    private static IServiceCollection AddHolmesSecurity(this IServiceCollection services)
    {
        services.AddScoped<IUserContext, HttpUserContext>();
        services.AddScoped<ICurrentUserInitializer, CurrentUserInitializer>();
        services.AddScoped<ICurrentUserAccess, CurrentUserAccess>();
        services.AddScoped<IAuthorizationHandler, GlobalAdminAuthorizationHandler>();

        return services;
    }
}
