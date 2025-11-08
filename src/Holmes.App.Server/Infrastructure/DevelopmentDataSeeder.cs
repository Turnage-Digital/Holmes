using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Infrastructure;

public sealed class DevelopmentDataSeeder : IHostedService
{
    private const string DefaultIssuer = "https://localhost:6001";
    private const string DefaultSubject = "admin";
    private const string DefaultEmail = "admin@holmes.dev";

    private readonly IServiceProvider _services;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        IServiceProvider services,
        IHostEnvironment environment,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _services = services;
        _environment = environment;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
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
            using var scope = _services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();

            var now = DateTimeOffset.UtcNow;
            var adminUserId = await EnsureAdminUserAsync(mediator, now, cancellationToken);
            await EnsureAdminRoleAsync(mediator, adminUserId, now, cancellationToken);
            await EnsureDemoCustomerAsync(mediator, customersDb, adminUserId, now, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed seeding development data");
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
            adminUserId,
            timestamp);
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
        var exists = await customersDb.CustomerDirectory
            .AsNoTracking()
            .AnyAsync(c => c.Name == demoCustomerName, cancellationToken);

        if (exists)
        {
            return;
        }

        var customerId = await mediator.Send(
            new RegisterCustomerCommand(demoCustomerName, timestamp),
            cancellationToken);

        await mediator.Send(
            new AssignCustomerAdminCommand(customerId, adminUserId, adminUserId, timestamp),
            cancellationToken);
    }
}
