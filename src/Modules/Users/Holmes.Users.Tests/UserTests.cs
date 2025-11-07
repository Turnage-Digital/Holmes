using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;

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
    public void RevokeRole_Prevents_Removing_Last_Admin()
    {
        var identity = new ExternalIdentity("https://issuer", "subject", "pwd", DateTimeOffset.UtcNow);
        var user = User.Register(UlidId.NewUlid(), identity, "user@example.com", "Example User", DateTimeOffset.UtcNow);
        var adminId = UlidId.NewUlid();
        user.GrantRole(UserRole.Admin, null, adminId, DateTimeOffset.UtcNow);

        Assert.That(() => user.RevokeRole(UserRole.Admin, null, adminId, DateTimeOffset.UtcNow),
            Throws.InvalidOperationException);
    }
}