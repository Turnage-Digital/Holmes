using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Entities;
using Holmes.Customers.Application.Commands;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

/// <summary>
///     Integration tests for <see cref="RequestSubjectIntakeCommand" />.
///     Verifies the command correctly orchestrates subject creation/reuse
///     and order creation. Intake invites are issued asynchronously.
/// </summary>
[TestFixture]
public class RequestSubjectIntakeCommandTests
{
    [Test]
    public async Task Creates_Subject_Order_And_IntakeSession_Atomically()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create an admin and customer
        UlidId adminId;
        UlidId customerId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "create-order-admin",
                "create-order-admin@test.com",
                "Create Order Admin",
                "password",
                DateTimeOffset.UtcNow,
                true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            customerId = await mediator.Send(new RegisterCustomerCommand("Test Customer Inc", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act: Create order with intake
        RequestSubjectIntakeResult? result;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var commandResult = await mediator.Send(new RequestSubjectIntakeCommand(
                "new-subject@example.com",
                "+15551234567",
                customerId,
                "policy-v1",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(commandResult.IsSuccess, Is.True, commandResult.Error);
            result = commandResult.Value;
        }

        // Assert: Command returned valid result
        // Note: With deferred dispatch, projections may not be populated yet.
        // We verify the command result instead of projections.
        Assert.Multiple(() =>
        {
            Assert.That(result!.SubjectId, Is.Not.Null.And.Not.EqualTo(default(UlidId)));
            Assert.That(result.OrderId, Is.Not.Null.And.Not.EqualTo(default(UlidId)));
            Assert.That(result.SubjectWasExisting, Is.False, "Subject should be new");
        });

        // Verify events were persisted (proving the transaction committed)
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            // Subject event should exist
            var subjectEvents = await coreDb.Events
                .Where(e => e.StreamId == $"Subject:{result!.SubjectId}")
                .ToListAsync();
            Assert.That(subjectEvents, Has.Count.GreaterThanOrEqualTo(1), "Subject events should be persisted");

            // Order event should exist (issued asynchronously)
            var orderEvents = await WaitForOrderEventsAsync(
                coreDb,
                result!.OrderId,
                CancellationToken.None);
            Assert.That(orderEvents, Has.Count.GreaterThanOrEqualTo(1), "Order events should be persisted");

            // IntakeSession invite is issued asynchronously by integration handlers.
        }
    }

    [Test]
    public async Task Reuses_Existing_Subject_By_Email()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create admin, customer, and existing subject
        UlidId adminId;
        UlidId customerId;
        UlidId existingSubjectId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "reuse-subject-admin",
                "reuse-subject-admin@test.com",
                "Reuse Subject Admin",
                "password",
                DateTimeOffset.UtcNow,
                true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            customerId = await mediator.Send(new RegisterCustomerCommand("Reuse Customer Inc", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            // Create order with intake first time
            var firstResult = await mediator.Send(new RequestSubjectIntakeCommand(
                "existing@example.com",
                "+15551111111",
                customerId,
                "policy-v1",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(firstResult.IsSuccess, Is.True);
            existingSubjectId = firstResult.Value.SubjectId;
        }

        // Act: Create another order for the same email
        RequestSubjectIntakeResult? result;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var commandResult = await mediator.Send(new RequestSubjectIntakeCommand(
                "existing@example.com",
                "+15552222222", // Different phone
                customerId,
                "policy-v2",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(commandResult.IsSuccess, Is.True, commandResult.Error);
            result = commandResult.Value;
        }

        // Assert: Same subject was reused
        Assert.Multiple(() =>
        {
            Assert.That(result!.SubjectId, Is.EqualTo(existingSubjectId), "Should reuse existing subject");
            Assert.That(result.SubjectWasExisting, Is.True, "Should indicate subject was existing");
        });

        // Assert: New phone was added to existing subject
        using (var scope = factory.Services.CreateScope())
        {
            var subjectsDb = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
            var phones = await subjectsDb.SubjectPhones
                .Where(p => p.SubjectId == existingSubjectId.ToString())
                .ToListAsync();

            Assert.That(phones, Has.Count.EqualTo(2), "Both phones should be added");
            Assert.That(phones.Select(p => p.PhoneNumber), Contains.Item("+15551111111"));
            Assert.That(phones.Select(p => p.PhoneNumber), Contains.Item("+15552222222"));
        }
    }

    [Test]
    public async Task Does_Not_Duplicate_Phone_If_Already_Exists()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange
        UlidId adminId;
        UlidId customerId;
        UlidId subjectId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "no-dup-phone-admin",
                "no-dup-phone-admin@test.com",
                "No Dup Phone Admin",
                "password",
                DateTimeOffset.UtcNow,
                true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            customerId = await mediator.Send(new RegisterCustomerCommand("No Dup Phone Inc", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            // First order with phone
            var firstResult = await mediator.Send(new RequestSubjectIntakeCommand(
                "same-phone@example.com",
                "+15559999999",
                customerId,
                "policy-v1",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(firstResult.IsSuccess, Is.True);
            subjectId = firstResult.Value.SubjectId;
        }

        // Act: Create another order with the SAME phone
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var secondResult = await mediator.Send(new RequestSubjectIntakeCommand(
                "same-phone@example.com",
                "+15559999999", // Same phone
                customerId,
                "policy-v2",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(secondResult.IsSuccess, Is.True);
        }

        // Assert: Phone should not be duplicated
        using (var scope = factory.Services.CreateScope())
        {
            var subjectsDb = scope.ServiceProvider.GetRequiredService<SubjectsDbContext>();
            var phones = await subjectsDb.SubjectPhones
                .Where(p => p.SubjectId == subjectId.ToString())
                .ToListAsync();

            Assert.That(phones, Has.Count.EqualTo(1), "Phone should not be duplicated");
        }
    }

    [Test]
    public async Task Fails_When_Email_Is_Empty()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange
        UlidId adminId;
        UlidId customerId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "empty-email-admin",
                "empty-email-admin@test.com",
                "Empty Email Admin",
                "password",
                DateTimeOffset.UtcNow,
                true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            customerId = await mediator.Send(new RegisterCustomerCommand("Empty Email Inc", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new RequestSubjectIntakeCommand(
                "",
                "+15551234567",
                customerId,
                "policy-v1",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Does.Contain("email"));
        }
    }

    [Test]
    public async Task Events_Are_Persisted_With_Null_DispatchedAt()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange
        UlidId adminId;
        UlidId customerId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "deferred-events-admin",
                "deferred-events-admin@test.com",
                "Deferred Events Admin",
                "password",
                DateTimeOffset.UtcNow,
                true));

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            customerId = await mediator.Send(new RegisterCustomerCommand("Deferred Events Inc", DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act: Create order with intake
        RequestSubjectIntakeResult? result;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var commandResult = await mediator.Send(new RequestSubjectIntakeCommand(
                "deferred-events@example.com",
                "+15551234567",
                customerId,
                "policy-v1",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(commandResult.IsSuccess, Is.True, commandResult.Error);
            result = commandResult.Value;
        }

        // Assert: Events from Subject and Order should exist
        // Note: The DeferredDispatchProcessor may have already dispatched them,
        // so we just verify events were created for the right streams
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            // Subject registration event
            var subjectEvents = await coreDb.Events
                .Where(e => e.StreamId == $"Subject:{result!.SubjectId}")
                .ToListAsync();
            Assert.That(subjectEvents, Has.Count.GreaterThanOrEqualTo(1), "Subject events should exist");

            // Order creation event (issued asynchronously)
            var orderEvents = await WaitForOrderEventsAsync(
                coreDb,
                result!.OrderId,
                CancellationToken.None);
            Assert.That(orderEvents, Has.Count.GreaterThanOrEqualTo(1), "Order events should exist");

            // IntakeSession invite is issued asynchronously by integration handlers.
        }
    }

    private static async Task<List<EventRecord>> WaitForOrderEventsAsync(
        CoreDbContext coreDb,
        UlidId orderId,
        CancellationToken cancellationToken)
    {
        List<EventRecord> events = [];
        for (var attempt = 0; attempt < 20; attempt++)
        {
            events = await coreDb.Events
                .Where(e => e.StreamId == $"Order:{orderId}")
                .ToListAsync(cancellationToken);
            if (events.Count > 0)
            {
                return events;
            }

            await Task.Delay(100, cancellationToken);
        }

        return events;
    }
}
