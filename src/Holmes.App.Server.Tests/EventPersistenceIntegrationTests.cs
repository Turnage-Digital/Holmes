using Holmes.Core.Contracts.Events;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

/// <summary>
///     Integration tests verifying the full event persistence flow:
///     Command → Aggregate → Domain Event → UnitOfWork → EventStore → MediatR → ProjectionHandler → Projection Table
/// </summary>
[TestFixture]
public class EventPersistenceIntegrationTests
{
    [Test]
    public async Task UserRegistration_PersistsEvent_And_UpdatesProjection()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Act: Register a user through the command handler
        string userId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var id = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "test-subject-001",
                "integration@test.com",
                "Integration Test User",
                "password123",
                DateTimeOffset.UtcNow,
                true));
            userId = id.ToString();
        }

        // Assert: Verify event was persisted to EventStore
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var events = await coreDb.Events
                .Where(e => e.StreamId == $"User:{userId}")
                .OrderBy(e => e.Version)
                .ToListAsync();

            Assert.That(events, Has.Count.GreaterThanOrEqualTo(1), "Expected at least one event to be persisted");

            var registeredEvent = events.FirstOrDefault(e => e.Name.Contains("UserRegistered"));
            Assert.That(registeredEvent, Is.Not.Null, "UserRegistered event should be persisted");
            Assert.Multiple(() =>
            {
                Assert.That(registeredEvent!.StreamType, Is.EqualTo("User"));
                Assert.That(registeredEvent.Payload, Does.Contain("integration@test.com"));
                Assert.That(registeredEvent.Version, Is.EqualTo(1));
            });
        }

        // Assert: Verify projection was updated via event handler
        using (var scope = factory.Services.CreateScope())
        {
            var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var projection = await usersDb.UserProjections
                .FirstOrDefaultAsync(p => p.UserId == userId);

            Assert.That(projection, Is.Not.Null, "User projection should exist after registration");
            Assert.Multiple(() =>
            {
                Assert.That(projection!.Email, Is.EqualTo("integration@test.com"));
                Assert.That(projection.DisplayName, Is.EqualTo("Integration Test User"));
                Assert.That(projection.Issuer, Is.EqualTo("https://issuer.test"));
                Assert.That(projection.Subject, Is.EqualTo("test-subject-001"));
                Assert.That(projection.Status, Is.EqualTo(UserStatus.Active));
            });
        }
    }

    [Test]
    public async Task UserSuspension_PersistsEvent_And_UpdatesProjectionStatus()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create a user and an admin
        UlidId userId;
        UlidId adminId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            userId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "user-to-suspend",
                "suspend@test.com",
                "User To Suspend",
                "password",
                DateTimeOffset.UtcNow,
                true));

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "admin-user",
                "admin@test.com",
                "Admin User",
                "password",
                DateTimeOffset.UtcNow,
                true));

            // Grant admin role
            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act: Suspend the user
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new SuspendUserCommand(userId, "Policy violation", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Assert: Verify suspension event was persisted
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var events = await coreDb.Events
                .Where(e => e.StreamId == $"User:{userId}")
                .OrderBy(e => e.Version)
                .ToListAsync();

            var suspendedEvent = events.FirstOrDefault(e => e.Name.Contains("UserSuspended"));
            Assert.That(suspendedEvent, Is.Not.Null, "UserSuspended event should be persisted");
            Assert.That(suspendedEvent!.Payload, Does.Contain("Policy violation"));
        }

        // Assert: Verify projection status was updated
        using (var scope = factory.Services.CreateScope())
        {
            var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var projection = await usersDb.UserProjections
                .FirstOrDefaultAsync(p => p.UserId == userId.ToString());

            Assert.That(projection, Is.Not.Null);
            Assert.That(projection!.Status, Is.EqualTo(UserStatus.Suspended));
        }
    }

    [Test]
    public async Task CustomerRegistration_PersistsEvent_And_UpdatesProjection()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create an admin user first
        UlidId adminId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "customer-admin",
                "customer-admin@test.com",
                "Customer Admin",
                "password",
                DateTimeOffset.UtcNow,
                true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act: Register a customer
        UlidId customerId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            customerId = await mediator.Send(new RegisterCustomerCommand("Acme Corporation", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Assert: Verify event was persisted
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var events = await coreDb.Events
                .Where(e => e.StreamId == $"Customer:{customerId}")
                .ToListAsync();

            Assert.That(events, Has.Count.GreaterThanOrEqualTo(1));

            var registeredEvent = events.FirstOrDefault(e => e.Name.Contains("CustomerRegistered"));
            Assert.That(registeredEvent, Is.Not.Null, "CustomerRegistered event should be persisted");
            Assert.Multiple(() =>
            {
                Assert.That(registeredEvent!.StreamType, Is.EqualTo("Customer"));
                Assert.That(registeredEvent.Payload, Does.Contain("Acme Corporation"));
            });
        }

        // Assert: Verify projection was updated
        using (var scope = factory.Services.CreateScope())
        {
            var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
            var projection = await customersDb.CustomerProjections
                .FirstOrDefaultAsync(p => p.CustomerId == customerId.ToString());

            Assert.That(projection, Is.Not.Null, "Customer projection should exist");
            Assert.Multiple(() =>
            {
                Assert.That(projection!.Name, Is.EqualTo("Acme Corporation"));
                Assert.That(projection.Status, Is.EqualTo(CustomerStatus.Active));
                Assert.That(projection.AdminCount, Is.EqualTo(0));
            });
        }
    }

    [Test]
    public async Task CustomerAdminAssignment_PersistsEvent_And_IncrementsAdminCount()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create admin, customer, and user to assign
        UlidId adminId;
        UlidId customerId;
        UlidId userToAssignId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test", "global-admin", "global-admin@test.com",
                "Global Admin", "password", DateTimeOffset.UtcNow, true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            customerId = await mediator.Send(new RegisterCustomerCommand("Test Customer", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            userToAssignId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test", "customer-admin-user", "customer-admin@test.com",
                "Customer Admin User", "password", DateTimeOffset.UtcNow, true));
        }

        // Act: Assign customer admin
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new AssignCustomerAdminCommand(customerId, userToAssignId, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Assert: Verify event and projection
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var events = await coreDb.Events
                .Where(e => e.StreamId == $"Customer:{customerId}")
                .ToListAsync();

            var assignedEvent = events.FirstOrDefault(e => e.Name.Contains("CustomerAdminAssigned"));
            Assert.That(assignedEvent, Is.Not.Null, "CustomerAdminAssigned event should be persisted");

            var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
            var projection = await customersDb.CustomerProjections
                .FirstOrDefaultAsync(p => p.CustomerId == customerId.ToString());

            Assert.That(projection!.AdminCount, Is.EqualTo(1), "Admin count should be incremented");
        }
    }

    [Test]
    public async Task SubjectRegistration_PersistsEvent_And_UpdatesProjection()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create an admin user
        UlidId adminId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test", "subject-admin", "subject-admin@test.com",
                "Subject Admin", "password", DateTimeOffset.UtcNow, true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act: Register a subject
        UlidId subjectId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            subjectId = await mediator.Send(new RegisterSubjectCommand(
                "Jane",
                "Doe",
                new DateOnly(1990, 5, 15),
                "jane.doe@example.com",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Assert: Verify event was persisted
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var events = await coreDb.Events
                .Where(e => e.StreamId == $"Subject:{subjectId}")
                .ToListAsync();

            Assert.That(events, Has.Count.GreaterThanOrEqualTo(1));

            var registeredEvent = events.FirstOrDefault(e => e.Name.Contains("SubjectRegistered"));
            Assert.That(registeredEvent, Is.Not.Null, "SubjectRegistered event should be persisted");
            Assert.Multiple(() =>
            {
                Assert.That(registeredEvent!.StreamType, Is.EqualTo("Subject"));
                Assert.That(registeredEvent.Payload, Does.Contain("Jane"));
                Assert.That(registeredEvent.Payload, Does.Contain("Doe"));
            });
        }

        // Assert: Verify projection was updated
        using (var scope = factory.Services.CreateScope())
        {
            var subjectsDb = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
            var projection = await subjectsDb.SubjectProjections
                .FirstOrDefaultAsync(p => p.SubjectId == subjectId.ToString());

            Assert.That(projection, Is.Not.Null, "Subject projection should exist");
            Assert.Multiple(() =>
            {
                Assert.That(projection!.GivenName, Is.EqualTo("Jane"));
                Assert.That(projection.FamilyName, Is.EqualTo("Doe"));
                Assert.That(projection.Email, Is.EqualTo("jane.doe@example.com"));
                Assert.That(projection.DateOfBirth, Is.EqualTo(new DateOnly(1990, 5, 15)));
                Assert.That(projection.IsMerged, Is.False);
                Assert.That(projection.AliasCount, Is.EqualTo(0));
            });
        }
    }

    [Test]
    public async Task EventStore_MaintainsStreamVersioning_AcrossMultipleOperations()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create admin and user
        UlidId userId;
        UlidId adminId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            userId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test", "versioning-test-user", "versioning@test.com",
                "Versioning Test", "password", DateTimeOffset.UtcNow, true));

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test", "versioning-admin", "versioning-admin@test.com",
                "Versioning Admin", "password", DateTimeOffset.UtcNow, true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act: Perform multiple operations on the same aggregate
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Suspend
            await mediator.Send(new SuspendUserCommand(userId, "First suspension", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            // Reactivate
            await mediator.Send(new ReactivateUserCommand(userId, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            // Suspend again
            await mediator.Send(new SuspendUserCommand(userId, "Second suspension", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Assert: Verify events have correct sequential versioning
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var events = await coreDb.Events
                .Where(e => e.StreamId == $"User:{userId}")
                .OrderBy(e => e.Version)
                .ToListAsync();

            Assert.That(events, Has.Count.EqualTo(4), "Should have 4 events: Register, Suspend, Reactivate, Suspend");

            // Verify sequential versioning
            for (var i = 0; i < events.Count; i++)
            {
                Assert.That(events[i].Version, Is.EqualTo(i + 1),
                    $"Event at index {i} should have version {i + 1}");
            }

            // Verify event order
            Assert.Multiple(() =>
            {
                Assert.That(events[0].Name, Does.Contain("UserRegistered"));
                Assert.That(events[1].Name, Does.Contain("UserSuspended"));
                Assert.That(events[2].Name, Does.Contain("UserReactivated"));
                Assert.That(events[3].Name, Does.Contain("UserSuspended"));
            });
        }
    }

    [Test]
    public async Task EventSerializer_RoundTrips_EventsCorrectly()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange & Act: Register a subject with rich data including Unicode characters
        UlidId adminId;
        UlidId subjectId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test", "serializer-admin", "serializer-admin@test.com",
                "Serializer Admin", "password", DateTimeOffset.UtcNow, true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            subjectId = await mediator.Send(new RegisterSubjectCommand(
                "María",
                "García-López",
                new DateOnly(1985, 12, 25),
                "maria.garcia@example.com",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Assert: Verify serialization round-trips correctly (Unicode may be escaped in JSON)
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var serializer = scope.ServiceProvider.GetRequiredService<IDomainEventSerializer>();

            var storedEvent = await coreDb.Events
                .FirstOrDefaultAsync(e => e.StreamId == $"Subject:{subjectId}");

            Assert.That(storedEvent, Is.Not.Null);
            Assert.That(storedEvent!.Name, Does.Contain("SubjectRegistered"));

            // Deserialize and verify the event round-trips correctly
            var deserialized = serializer.Deserialize(storedEvent.Payload, storedEvent.Name);
            Assert.That(deserialized, Is.Not.Null);

            // Use reflection to verify deserialized values (Unicode should be properly decoded)
            var givenNameProp = deserialized.GetType().GetProperty("GivenName");
            var familyNameProp = deserialized.GetType().GetProperty("FamilyName");
            var emailProp = deserialized.GetType().GetProperty("Email");
            var dobProp = deserialized.GetType().GetProperty("DateOfBirth");

            Assert.Multiple(() =>
            {
                Assert.That(givenNameProp?.GetValue(deserialized)?.ToString(), Is.EqualTo("María"),
                    "Unicode characters should round-trip correctly");
                Assert.That(familyNameProp?.GetValue(deserialized)?.ToString(), Is.EqualTo("García-López"),
                    "Unicode and special characters should round-trip correctly");
                Assert.That(emailProp?.GetValue(deserialized)?.ToString(), Is.EqualTo("maria.garcia@example.com"));
                Assert.That(dobProp?.GetValue(deserialized), Is.EqualTo(new DateOnly(1985, 12, 25)),
                    "DateOnly should serialize/deserialize correctly");
            });
        }
    }
}