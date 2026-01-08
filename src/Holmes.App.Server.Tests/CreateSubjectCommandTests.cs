using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

/// <summary>
///     Integration tests for <see cref="CreateSubjectCommand" />.
///     Verifies the command creates or reuses subjects from email input
///     and normalizes phone numbers without duplication.
/// </summary>
[TestFixture]
public class CreateSubjectCommandTests
{
    [Test]
    public async Task Creates_Subject_From_Email_And_Persists_Events()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create an admin and customer
        UlidId adminId;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            adminId = await mediator.Send(new RegisterExternalUserCommand(
                "https://issuer.test",
                "ensure-subject-admin",
                "ensure-subject-admin@test.com",
                "Ensure Subject Admin",
                "password",
                DateTimeOffset.UtcNow,
                true)
            {
                UserId = SystemActors.System
            });

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act: Create subject from email
        CreateSubjectResult? result;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var commandResult = await mediator.Send(new CreateSubjectCommand(
                "new-subject@example.com",
                "+15551234567",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(commandResult.IsSuccess, Is.True, commandResult.Error);
            result = commandResult.Value;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result!.SubjectId, Is.Not.EqualTo(default(UlidId)));
            Assert.That(result.SubjectWasExisting, Is.False, "Subject should be new");
        });

        // Verify events were persisted (proving the transaction committed)
        using (var scope = factory.Services.CreateScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            var subjectEvents = await coreDb.Events
                .Where(e => e.StreamId == $"Subject:{result!.SubjectId}")
                .ToListAsync();
            Assert.That(subjectEvents, Has.Count.GreaterThanOrEqualTo(1), "Subject events should be persisted");
        }
    }

    [Test]
    public async Task Reuses_Subject_By_Email()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange: Create admin and existing subject
        UlidId adminId;
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
                true)
            {
                UserId = SystemActors.System
            });

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            var firstResult = await mediator.Send(new CreateSubjectCommand(
                "existing@example.com",
                "+15551111111",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(firstResult.IsSuccess, Is.True);
            existingSubjectId = firstResult.Value.SubjectId;
        }

        // Act: Create again with same email
        CreateSubjectResult? result;
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var commandResult = await mediator.Send(new CreateSubjectCommand(
                "existing@example.com",
                "+15552222222",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(commandResult.IsSuccess, Is.True, commandResult.Error);
            result = commandResult.Value;
        }

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
    public async Task Does_Not_Duplicate_Phone_For_Email()
    {
        await using var factory = new HolmesWebApplicationFactory();

        // Arrange
        UlidId adminId;
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
                true)
            {
                UserId = SystemActors.System
            });

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            var firstResult = await mediator.Send(new CreateSubjectCommand(
                "same-phone@example.com",
                "+15559999999",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            Assert.That(firstResult.IsSuccess, Is.True);
            subjectId = firstResult.Value.SubjectId;
        }

        // Act: Ensure again with the same phone
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var secondResult = await mediator.Send(new CreateSubjectCommand(
                "same-phone@example.com",
                "+15559999999",
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
                true)
            {
                UserId = SystemActors.System
            });

            await mediator.Send(new GrantUserRoleCommand(adminId, UserRole.Admin, null, DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });
        }

        // Act
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new CreateSubjectCommand(
                "",
                "+15551234567",
                DateTimeOffset.UtcNow)
            {
                UserId = adminId.ToString()
            });

            // Assert
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(ResultErrors.Validation));
        }
    }
}