using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Holmes.Identity.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Holmes.Identity.Server;

internal static class SeedData
{
    public static async Task EnsureSeedDataAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = services.CreateAsyncScope();

        var configurationDb = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var operationalDb = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var passwordOptions = scope.ServiceProvider.GetRequiredService<IOptions<PasswordPolicyOptions>>().Value;

        await configurationDb.Database.MigrateAsync(cancellationToken);
        await operationalDb.Database.MigrateAsync(cancellationToken);
        await appDb.Database.MigrateAsync(cancellationToken);

        await SeedIdentityServerConfigAsync(configurationDb, cancellationToken);
        await SeedDefaultUsersAsync(userManager, roleManager, timeProvider, passwordOptions);
    }

    private static async Task SeedIdentityServerConfigAsync(
        ConfigurationDbContext configurationDb,
        CancellationToken cancellationToken
    )
    {
        if (!configurationDb.IdentityResources.Any())
        {
            foreach (var resource in Config.IdentityResources)
            {
                configurationDb.IdentityResources.Add(resource.ToEntity());
            }
        }

        if (!configurationDb.ApiScopes.Any())
        {
            foreach (var scope in Config.ApiScopes)
            {
                configurationDb.ApiScopes.Add(scope.ToEntity());
            }
        }

        if (!configurationDb.ApiResources.Any())
        {
            foreach (var apiResource in Config.ApiResources)
            {
                configurationDb.ApiResources.Add(apiResource.ToEntity());
            }
        }

        if (!configurationDb.Clients.Any())
        {
            foreach (var client in Config.Clients)
            {
                configurationDb.Clients.Add(client.ToEntity());
            }
        }

        await configurationDb.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDefaultUsersAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TimeProvider timeProvider,
        PasswordPolicyOptions passwordOptions
    )
    {
        string[] roles = ["Admin", "Operations"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var now = timeProvider.GetUtcNow();
        var passwordExpires = now.AddDays(passwordOptions.ExpirationDays);

        const string adminEmail = "admin@holmes.dev";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Dev Admin",
                LastPasswordChangedAt = now,
                PasswordExpires = passwordExpires
            };

            const string seedPassword = "ChangeMe123!";
            var result = await userManager.CreateAsync(adminUser, seedPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create seed admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        const string opsEmail = "ops@holmes.dev";
        var opsUser = await userManager.FindByEmailAsync(opsEmail);
        if (opsUser is null)
        {
            opsUser = new ApplicationUser
            {
                UserName = opsEmail,
                Email = opsEmail,
                EmailConfirmed = true,
                DisplayName = "Ops User",
                LastPasswordChangedAt = now,
                PasswordExpires = passwordExpires
            };

            const string seedPassword = "ChangeMe123!";
            var result = await userManager.CreateAsync(opsUser, seedPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create seed ops user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await userManager.AddToRoleAsync(opsUser, "Operations");
        }
    }
}