using System.Security.Claims;
using Duende.IdentityServer.Models;
using Holmes.Identity.Server.Data;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Holmes.Identity.Server.Tests;

public class ProfileServiceTests
{
    private ProfileService _profileService = null!;
    private FakeTimeProvider _timeProvider = null!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;

    [SetUp]
    public void SetUp()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));

        _profileService = new ProfileService(_userManagerMock.Object, _timeProvider);
    }

    [Test]
    public async Task IsActiveAsync_WhenUserNotFound_ReturnsFalse()
    {
        var context = CreateIsActiveContext("nonexistent-user");
        _userManagerMock
            .Setup(x => x.FindByIdAsync("nonexistent-user"))
            .ReturnsAsync((ApplicationUser?)null);

        await _profileService.IsActiveAsync(context);

        Assert.That(context.IsActive, Is.False);
    }

    [Test]
    public async Task IsActiveAsync_WhenEmailNotConfirmed_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            EmailConfirmed = false
        };
        var context = CreateIsActiveContext("user1");
        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        await _profileService.IsActiveAsync(context);

        Assert.That(context.IsActive, Is.False);
    }

    [Test]
    public async Task IsActiveAsync_WhenPasswordExpired_ReturnsFalse()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            EmailConfirmed = true,
            PasswordExpires = _timeProvider.GetUtcNow().AddDays(-1)
        };
        var context = CreateIsActiveContext("user1");
        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        await _profileService.IsActiveAsync(context);

        Assert.That(context.IsActive, Is.False);
    }

    [Test]
    public async Task IsActiveAsync_WhenPasswordNotExpired_ReturnsTrue()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            EmailConfirmed = true,
            PasswordExpires = _timeProvider.GetUtcNow().AddDays(30)
        };
        var context = CreateIsActiveContext("user1");
        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        await _profileService.IsActiveAsync(context);

        Assert.That(context.IsActive, Is.True);
    }

    [Test]
    public async Task IsActiveAsync_WhenPasswordExpiresIsNull_ReturnsTrue()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            EmailConfirmed = true,
            PasswordExpires = null
        };
        var context = CreateIsActiveContext("user1");
        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        await _profileService.IsActiveAsync(context);

        Assert.That(context.IsActive, Is.True);
    }

    [Test]
    public async Task IsActiveAsync_WhenSubjectIdMissing_ReturnsFalse()
    {
        var context = new IsActiveContext(
            new ClaimsPrincipal(new ClaimsIdentity()),
            new Client(),
            "test");

        await _profileService.IsActiveAsync(context);

        Assert.That(context.IsActive, Is.False);
    }

    private static IsActiveContext CreateIsActiveContext(string subjectId)
    {
        var claims = new List<Claim>
        {
            new(JwtClaimTypes.Subject, subjectId)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        return new IsActiveContext(principal, new Client(), "test");
    }
}