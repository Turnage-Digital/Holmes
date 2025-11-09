using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Infrastructure;

public sealed class DevelopmentDataSeeder(
    IServiceProvider services,
    IHostEnvironment environment,
    ILogger<DevelopmentDataSeeder> logger
)
    : IHostedService
{
    private const string DefaultIssuer = "https://localhost:6001";
    private const string DefaultSubject = "admin";
    private const string DefaultEmail = "admin@holmes.dev";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        _ = SeedAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();

            var now = DateTimeOffset.UtcNow;
            var adminUserId = await EnsureAdminUserAsync(mediator, now, cancellationToken);
            await EnsureAdminRoleAsync(mediator, adminUserId, now, cancellationToken);
            await EnsureDemoCustomerAsync(mediator, customersDb, adminUserId, now, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed seeding development data");
        }
    }

    private static async Task<UlidId> EnsureAdminUserAsync(
        IMediator mediator,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var command = new RegisterExternalUserCommand(
            DefaultIssuer,
            DefaultSubject,
            DefaultEmail,
            "Dev Admin",
            "pwd",
            timestamp);
        return await mediator.Send(command, cancellationToken);
    }

    private static async Task EnsureAdminRoleAsync(
        IMediator mediator,
        UlidId adminUserId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var grant = new GrantUserRoleCommand(
            adminUserId,
            UserRole.Admin,
            null,
            timestamp)
        {
            UserId = adminUserId.ToString()
        };
        await mediator.Send(grant, cancellationToken);
    }

    private static async Task EnsureDemoCustomerAsync(
        IMediator mediator,
        CustomersDbContext customersDb,
        UlidId adminUserId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        const string demoCustomerName = "Holmes Demo Customer";
        var directory = await customersDb.CustomerDirectory
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == demoCustomerName, cancellationToken);

        UlidId customerId;
        if (directory is null)
        {
            customerId = await mediator.Send(
                new RegisterCustomerCommand(demoCustomerName, timestamp),
                cancellationToken);

            var assign = new AssignCustomerAdminCommand(customerId, adminUserId, timestamp)
            {
                UserId = adminUserId.ToString()
            };
            await mediator.Send(assign, cancellationToken);
        }
        else
        {
            customerId = UlidId.Parse(directory.CustomerId);
        }

        await EnsureDemoCustomerProfileAsync(customersDb, customerId.ToString(), timestamp, cancellationToken);
    }

    private static async Task EnsureDemoCustomerProfileAsync(
        CustomersDbContext customersDb,
        string customerId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var exists = await customersDb.CustomerProfiles
            .AnyAsync(p => p.CustomerId == customerId, cancellationToken);

        if (exists)
        {
            return;
        }

        var profile = new CustomerProfileDb
        {
            CustomerId = customerId,
            TenantId = Ulid.NewUlid().ToString(),
            PolicySnapshotId = "policy-default",
            BillingEmail = "billing@holmes.dev",
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        var contact = new CustomerContactDb
        {
            ContactId = Ulid.NewUlid().ToString(),
            CustomerId = customerId,
            Name = "Dev Ops",
            Email = "ops@holmes.dev",
            Role = "Operations",
            CreatedAt = timestamp
        };

        customersDb.CustomerProfiles.Add(profile);
        customersDb.CustomerContacts.Add(contact);
        await customersDb.SaveChangesAsync(cancellationToken);
    }
}
