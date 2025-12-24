using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Services.Application.Commands;
using Holmes.Services.Domain;
using Holmes.SlaClocks.Application.Commands;
using Holmes.SlaClocks.Domain;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Holmes.Orders.Application.Commands;
using Holmes.Orders.Infrastructure.Sql;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server;

public sealed class SeedData(
    IServiceProvider services,
    IHostEnvironment environment,
    ILogger<SeedData> logger
) : IHostedService
{
    private const string DefaultIssuer = "https://localhost:6001";
    private const string DefaultSubject = "admin";
    private const string DefaultEmail = "admin@holmes.dev";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        await SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
            var subjectsDb = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
            var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var workflowDb = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

            if (await HasExistingSeedAsync(usersDb, cancellationToken))
            {
                logger.LogInformation("Development data already present; skipping seed.");
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var adminUserId = await EnsureAdminUserAsync(mediator, now, cancellationToken);
            await EnsureAdminRoleAsync(mediator, adminUserId, now, cancellationToken);
            await EnsureAdminApprovedAsync(mediator, adminUserId, now, cancellationToken);
            var customerId = await EnsureDemoCustomerAsync(mediator, customersDb, adminUserId, now, cancellationToken);
            var subjectIds = await EnsureDemoSubjectsAsync(mediator, subjectsDb, adminUserId, now, cancellationToken);
            await EnsureDemoOrdersAsync(services, workflowDb, customerId, subjectIds, adminUserId, now,
                cancellationToken);

            logger.LogInformation("Development seed data created successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed seeding development data");
        }
    }

    private static async Task<bool> HasExistingSeedAsync(
        UsersDbContext usersDb,
        CancellationToken cancellationToken
    )
    {
        return await usersDb.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == DefaultEmail, cancellationToken);
    }

    private static async Task<UlidId> EnsureAdminUserAsync(
        IMediator mediator,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
    {
        var command = new RegisterExternalUserCommand(
            DefaultIssuer,
            DefaultSubject,
            DefaultEmail,
            "Dev Admin",
            "pwd",
            timestamp,
            true);
        return await mediator.Send(command, cancellationToken);
    }

    private static async Task EnsureAdminRoleAsync(
        IMediator mediator,
        UlidId adminUserId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
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

    private static async Task EnsureAdminApprovedAsync(
        IMediator mediator,
        UlidId adminUserId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
    {
        var approve = new ReactivateUserCommand(adminUserId, timestamp)
        {
            UserId = adminUserId.ToString()
        };
        await mediator.Send(approve, cancellationToken);
    }

    private static async Task<UlidId> EnsureDemoCustomerAsync(
        IMediator mediator,
        CustomersDbContext customersDb,
        UlidId adminUserId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
    {
        const string demoCustomerName = "Holmes Demo Customer";
        var directory = await customersDb.CustomerProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == demoCustomerName, cancellationToken);

        UlidId customerId;
        if (directory is null)
        {
            var create = new RegisterCustomerCommand(demoCustomerName, timestamp)
            {
                UserId = adminUserId.ToString()
            };
            customerId = await mediator.Send(create, cancellationToken);

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
        return customerId;
    }

    private static async Task EnsureDemoCustomerProfileAsync(
        CustomersDbContext customersDb,
        string customerId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
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

    private static async Task<List<UlidId>> EnsureDemoSubjectsAsync(
        IMediator mediator,
        SubjectsDbContext subjectsDb,
        UlidId adminUserId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
    {
        var subjectIds = new List<UlidId>();

        // Sample subjects for testing
        var subjects = new[]
        {
            ("Casey", "Holmes", new DateOnly(1994, 4, 12), "casey@example.com"),
            ("Jordan", "Smith", new DateOnly(1988, 7, 22), "jordan.smith@example.com"),
            ("Taylor", "Johnson", new DateOnly(1991, 11, 3), "taylor.j@example.com"),
            ("Morgan", "Williams", new DateOnly(1985, 2, 14), "morgan.w@example.com"),
            ("Riley", "Brown", new DateOnly(1996, 9, 8), "riley.brown@example.com"),
            ("Alex", "Davis", new DateOnly(1990, 5, 30), "alex.d@example.com"),
            ("Jamie", "Miller", new DateOnly(1993, 12, 17), "jamie.miller@example.com"),
            ("Quinn", "Wilson", new DateOnly(1987, 6, 25), "quinn.w@example.com"),
            ("Drew", "Moore", new DateOnly(1995, 1, 9), "drew.moore@example.com"),
            ("Avery", "Taylor", new DateOnly(1992, 8, 11), "avery.t@example.com")
        };

        foreach (var (givenName, familyName, dob, email) in subjects)
        {
            var existing = await subjectsDb.SubjectProjections
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.GivenName == givenName && s.FamilyName == familyName,
                    cancellationToken);

            if (existing is not null)
            {
                subjectIds.Add(UlidId.Parse(existing.SubjectId));
                continue;
            }

            var createSubject = new RegisterSubjectCommand(
                givenName,
                familyName,
                dob,
                email,
                timestamp)
            {
                UserId = adminUserId.ToString()
            };
            var subjectId = await mediator.Send(createSubject, cancellationToken);
            subjectIds.Add(subjectId);
        }

        return subjectIds;
    }

    private static async Task EnsureDemoOrdersAsync(
        IServiceProvider services,
        WorkflowDbContext workflowDb,
        UlidId customerId,
        List<UlidId> subjectIds,
        UlidId adminUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken
    )
    {
        // Check if we already have orders
        var existingOrderCount = await workflowDb.Orders.CountAsync(cancellationToken);
        if (existingOrderCount > 0)
        {
            return;
        }

        const string policySnapshotId = "policy-default";

        // Create orders in various states
        var orderScenarios = new[]
        {
            // Recent orders - Created status
            (subjectIds[0], TimeSpan.FromMinutes(-30), "Created"),
            (subjectIds[1], TimeSpan.FromHours(-2), "Created"),

            // Invited status - waiting for subject to start intake
            (subjectIds[2], TimeSpan.FromHours(-6), "Invited"),
            (subjectIds[3], TimeSpan.FromDays(-1), "Invited"),

            // Intake in progress
            (subjectIds[4], TimeSpan.FromHours(-4), "IntakeInProgress"),

            // Intake complete - ready for review
            (subjectIds[5], TimeSpan.FromDays(-2), "IntakeComplete"),

            // Ready for fulfillment / FulfillmentInProgress (these will have services)
            (subjectIds[6], TimeSpan.FromDays(-3), "ReadyForFulfillment"),
            (subjectIds[7], TimeSpan.FromDays(-4), "ReadyForFulfillment"),
            (subjectIds[8], TimeSpan.FromDays(-5), "ReadyForFulfillment"),

            // Blocked order
            (subjectIds[9], TimeSpan.FromDays(-7), "Blocked")
        };

        foreach (var (subjectId, ageOffset, targetStatus) in orderScenarios)
        {
            // Use fresh scope for each order to avoid EF tracking conflicts
            using var scope = services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var orderId = UlidId.NewUlid();
            var createdAt = now.Add(ageOffset);

            // Create the order
            var createCommand = new CreateOrderCommand(
                orderId,
                subjectId,
                customerId,
                policySnapshotId,
                createdAt,
                "standard")
            {
                UserId = adminUserId.ToString()
            };
            await mediator.Send(createCommand, cancellationToken);

            // Progress the order to the target status
            await ProgressOrderToStatusAsync(mediator, orderId, customerId, subjectId,
                targetStatus, createdAt, adminUserId, cancellationToken);
        }
    }

    private static async Task ProgressOrderToStatusAsync(
        IMediator mediator,
        UlidId orderId,
        UlidId customerId,
        UlidId subjectId,
        string targetStatus,
        DateTimeOffset createdAt,
        UlidId adminUserId,
        CancellationToken cancellationToken
    )
    {
        // Start Overall SLA clock for all orders
        await mediator.Send(new StartSlaClockCommand(
                orderId, customerId, ClockKind.Overall, createdAt)
            { UserId = adminUserId.ToString() }, cancellationToken);

        if (targetStatus == "Created")
        {
            return; // Already at Created
        }

        // Create a fake intake session ID for order workflow tracking
        var intakeSessionId = UlidId.NewUlid();

        // Record invite (moves to Invited)
        var inviteTimestamp = createdAt.AddMinutes(5);
        var inviteCommand =
            new RecordOrderInviteCommand(orderId, intakeSessionId, inviteTimestamp, "Intake invitation sent")
            {
                UserId = adminUserId.ToString()
            };
        await mediator.Send(inviteCommand, cancellationToken);

        // Start Intake SLA clock
        await mediator.Send(new StartSlaClockCommand(
                orderId, customerId, ClockKind.Intake, inviteTimestamp)
            { UserId = adminUserId.ToString() }, cancellationToken);

        if (targetStatus == "Invited")
        {
            return;
        }

        // Mark intake started (moves to IntakeInProgress)
        var startTimestamp = inviteTimestamp.AddHours(1);
        var startCommand =
            new MarkOrderIntakeStartedCommand(orderId, intakeSessionId, startTimestamp, "Subject began intake")
            {
                UserId = adminUserId.ToString()
            };
        await mediator.Send(startCommand, cancellationToken);

        if (targetStatus == "IntakeInProgress")
        {
            return;
        }

        // Mark intake submitted (moves to IntakeComplete)
        var submitTimestamp = startTimestamp.AddHours(2);
        var submitCommand =
            new MarkOrderIntakeSubmittedCommand(orderId, intakeSessionId, submitTimestamp, "Intake form completed")
            {
                UserId = adminUserId.ToString()
            };
        await mediator.Send(submitCommand, cancellationToken);

        // Complete the Intake clock
        await mediator.Send(new CompleteSlaClockCommand(
                orderId, ClockKind.Intake, submitTimestamp)
            { UserId = adminUserId.ToString() }, cancellationToken);

        if (targetStatus == "IntakeComplete")
        {
            return;
        }

        // Mark ready for fulfillment
        var readyTimestamp = submitTimestamp.AddHours(1);
        var readyCommand =
            new MarkOrderReadyForFulfillmentCommand(orderId, readyTimestamp, "Intake reviewed and approved")
            {
                UserId = adminUserId.ToString()
            };
        await mediator.Send(readyCommand, cancellationToken);

        // Start Fulfillment SLA clock
        await mediator.Send(new StartSlaClockCommand(
                orderId, customerId, ClockKind.Fulfillment, readyTimestamp)
            { UserId = adminUserId.ToString() }, cancellationToken);

        // Create services for fulfillment dashboard
        await CreateServicesForOrderAsync(
            mediator, orderId, customerId, adminUserId, readyTimestamp, cancellationToken);

        if (targetStatus == "ReadyForFulfillment" || targetStatus == "FulfillmentInProgress")
        {
            return;
        }

        // Block the order (we don't have BeginFulfillment command, so skip to block from ReadyForFulfillment)
        if (targetStatus == "Blocked")
        {
            var blockTimestamp = readyTimestamp.AddHours(1);
            var blockCommand = new BlockOrderCommand(orderId, "Awaiting additional documentation", blockTimestamp)
            {
                UserId = adminUserId.ToString()
            };
            await mediator.Send(blockCommand, cancellationToken);
        }
    }

    private static async Task CreateServicesForOrderAsync(
        IMediator mediator,
        UlidId orderId,
        UlidId customerId,
        UlidId adminUserId,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
    {
        // Create a standard set of services for demo purposes
        // Tier 1: Identity verification services (run first)
        var tier1Services = new[]
        {
            ("SSN_TRACE", ServiceCategory.Identity),
            ("ADDR_VERIFY", ServiceCategory.Identity)
        };

        // Tier 2: Core searches (run after tier 1)
        var tier2Services = new[]
        {
            ("FED_CRIM", ServiceCategory.Criminal),
            ("STATE_CRIM", ServiceCategory.Criminal),
            ("COUNTY_CRIM", ServiceCategory.Criminal)
        };

        // Tier 3: Employment and education verifications
        var tier3Services = new[]
        {
            ("TWN_EMP", ServiceCategory.Employment),
            ("EDU_VERIFY", ServiceCategory.Education)
        };

        foreach (var (code, _) in tier1Services)
        {
            await mediator.Send(new CreateServiceCommand(
                    orderId, customerId, code, 1, null, null, timestamp)
                { UserId = adminUserId.ToString() }, cancellationToken);
        }

        foreach (var (code, _) in tier2Services)
        {
            await mediator.Send(new CreateServiceCommand(
                    orderId, customerId, code, 2, null, null, timestamp.AddSeconds(1))
                { UserId = adminUserId.ToString() }, cancellationToken);
        }

        foreach (var (code, _) in tier3Services)
        {
            await mediator.Send(new CreateServiceCommand(
                    orderId, customerId, code, 3, null, null, timestamp.AddSeconds(2))
                { UserId = adminUserId.ToString() }, cancellationToken);
        }
    }
}