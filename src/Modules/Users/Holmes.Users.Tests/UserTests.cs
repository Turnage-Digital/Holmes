using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.EventHandlers;
using Holmes.Users.Domain;
using Holmes.Users.Domain.Events;
using Holmes.Users.Domain.ValueObjects;
using Holmes.Users.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Holmes.Users.Tests;

[TestFixture]
public class UserTests
{
    [Test]
    public void Register_Creates_Active_User()
    {
        var identity = new ExternalIdentity("https://issuer", "subject", "pwd", DateTimeOffset.UtcNow);
        var user = User.Register(UlidId.NewUlid(), identity, "user@example.com", "Example User", DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(user.Status, Is.EqualTo(UserStatus.Active));
            Assert.That(user.ExternalIdentities.Single().Issuer, Is.EqualTo("https://issuer"));
        });
    }

    [Test]
    public void ActivatePendingInvitation_Sets_User_To_Active()
    {
        var user = User.Invite(UlidId.NewUlid(), "user@example.com", "Example User", DateTimeOffset.UtcNow);

        user.ActivatePendingInvitation(DateTimeOffset.UtcNow);

        Assert.That(user.Status, Is.EqualTo(UserStatus.Active));
    }

    [Test]
    public void RevokeRole_Prevents_Removing_Last_Admin()
    {
        var identity = new ExternalIdentity("https://issuer", "subject", "pwd", DateTimeOffset.UtcNow);
        var user = User.Register(UlidId.NewUlid(), identity, "user@example.com", "Example User", DateTimeOffset.UtcNow);
        var adminId = UlidId.NewUlid();
        user.GrantRole(UserRole.Admin, null, adminId, DateTimeOffset.UtcNow);

        Assert.That(() => user.RevokeRole(UserRole.Admin, null, adminId, DateTimeOffset.UtcNow),
            Throws.InvalidOperationException);
    }

    [Test]
    public async Task UserProjectionHandler_UserInvited_WritesProjection()
    {
        await using var context = CreateUsersDbContext();
        var writer = new UserProjectionWriter(context, NullLogger<UserProjectionWriter>.Instance);
        var handler = new UserProjectionHandler(writer);
        var userId = UlidId.NewUlid();

        var @event = new UserInvited(userId, "user@example.com", "Test User", DateTimeOffset.UtcNow);
        await handler.Handle(@event, CancellationToken.None);

        var projection = await context.UserProjections.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(projection.UserId, Is.EqualTo(userId.ToString()));
            Assert.That(projection.Email, Is.EqualTo("user@example.com"));
            Assert.That(projection.DisplayName, Is.EqualTo("Test User"));
            Assert.That(projection.Status, Is.EqualTo(UserStatus.Invited));
        });
    }

    [Test]
    public async Task UserProjectionHandler_UserRegistered_WritesProjection()
    {
        await using var context = CreateUsersDbContext();
        var writer = new UserProjectionWriter(context, NullLogger<UserProjectionWriter>.Instance);
        var handler = new UserProjectionHandler(writer);
        var userId = UlidId.NewUlid();

        var @event = new UserRegistered(
            userId,
            "https://issuer",
            "external-subject",
            "user@example.com",
            "Test User",
            "password",
            DateTimeOffset.UtcNow);
        await handler.Handle(@event, CancellationToken.None);

        var projection = await context.UserProjections.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(projection.UserId, Is.EqualTo(userId.ToString()));
            Assert.That(projection.Email, Is.EqualTo("user@example.com"));
            Assert.That(projection.Issuer, Is.EqualTo("https://issuer"));
            Assert.That(projection.Subject, Is.EqualTo("external-subject"));
            Assert.That(projection.Status, Is.EqualTo(UserStatus.Active));
        });
    }

    [Test]
    public async Task UserProjectionHandler_UserSuspendedAndReactivated_UpdatesStatus()
    {
        await using var context = CreateUsersDbContext();
        var writer = new UserProjectionWriter(context, NullLogger<UserProjectionWriter>.Instance);
        var handler = new UserProjectionHandler(writer);
        var userId = UlidId.NewUlid();
        var adminId = UlidId.NewUlid();

        // First create the user
        var registered = new UserRegistered(
            userId,
            "https://issuer",
            "external-subject",
            "user@example.com",
            "Test User",
            "password",
            DateTimeOffset.UtcNow);
        await handler.Handle(registered, CancellationToken.None);

        // Suspend
        var suspended = new UserSuspended(userId, "Policy violation", adminId, DateTimeOffset.UtcNow);
        await handler.Handle(suspended, CancellationToken.None);

        var projection = await context.UserProjections.SingleAsync();
        Assert.That(projection.Status, Is.EqualTo(UserStatus.Suspended));

        // Reactivate
        var reactivated = new UserReactivated(userId, adminId, DateTimeOffset.UtcNow);
        await handler.Handle(reactivated, CancellationToken.None);

        projection = await context.UserProjections.SingleAsync();
        Assert.That(projection.Status, Is.EqualTo(UserStatus.Active));
    }

    private static UsersDbContext CreateUsersDbContext()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase($"users-tests-{Guid.NewGuid()}")
            .Options;
        return new UsersDbContext(options);
    }
}