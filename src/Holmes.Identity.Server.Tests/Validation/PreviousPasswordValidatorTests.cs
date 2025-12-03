using Holmes.Identity.Server.Data;
using Holmes.Identity.Server.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Holmes.Identity.Server.Tests.Validation;

public class PreviousPasswordValidatorTests
{
    private ApplicationDbContext _dbContext = null!;
    private IOptions<PasswordPolicyOptions> _options = null!;
    private Mock<IPasswordHasher<ApplicationUser>> _passwordHasherMock = null!;
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private PreviousPasswordValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(dbOptions);

        _passwordHasherMock = new Mock<IPasswordHasher<ApplicationUser>>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, _passwordHasherMock.Object, null!, null!, null!, null!, null!, null!);

        _options = Options.Create(new PasswordPolicyOptions
        {
            ExpirationDays = 90,
            PreviousPasswordCount = 10
        });

        _validator = new PreviousPasswordValidator(_dbContext, _options);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task ValidateAsync_WithNullPassword_ReturnsSuccess()
    {
        var user = new ApplicationUser { Id = "user1", UserName = "test@example.com" };

        var result = await _validator.ValidateAsync(_userManagerMock.Object, user, null);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task ValidateAsync_WithEmptyPassword_ReturnsSuccess()
    {
        var user = new ApplicationUser { Id = "user1", UserName = "test@example.com" };

        var result = await _validator.ValidateAsync(_userManagerMock.Object, user, string.Empty);

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task ValidateAsync_WhenPasswordMatchesCurrent_ReturnsError()
    {
        var user = new ApplicationUser
        {
            Id = "user1",
            UserName = "test@example.com",
            PasswordHash = "hashedPassword"
        };

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(user, "hashedPassword", "newPassword"))
            .Returns(PasswordVerificationResult.Success);

        var result = await _validator.ValidateAsync(_userManagerMock.Object, user, "newPassword");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Errors.Any(e => e.Code == "PasswordReused"), Is.True);
    }

    [Test]
    public async Task ValidateAsync_WhenPasswordMatchesPreviousPassword_ReturnsError()
    {
        var userId = "user1";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "test@example.com",
            PasswordHash = "currentHash"
        };

        _dbContext.UserPreviousPasswords.Add(new UserPreviousPassword
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = "previousHash",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
        });
        await _dbContext.SaveChangesAsync();

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(user, "currentHash", "newPassword"))
            .Returns(PasswordVerificationResult.Failed);
        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(user, "previousHash", "newPassword"))
            .Returns(PasswordVerificationResult.Success);

        var result = await _validator.ValidateAsync(_userManagerMock.Object, user, "newPassword");

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Errors.Any(e => e.Code == "PasswordPreviouslyUsed"), Is.True);
    }

    [Test]
    public async Task ValidateAsync_WhenPasswordIsNew_ReturnsSuccess()
    {
        var userId = "user1";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "test@example.com",
            PasswordHash = "currentHash"
        };

        _dbContext.UserPreviousPasswords.Add(new UserPreviousPassword
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = "previousHash",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
        });
        await _dbContext.SaveChangesAsync();

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(user, It.IsAny<string>(), "newPassword"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await _validator.ValidateAsync(_userManagerMock.Object, user, "newPassword");

        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task ValidateAsync_OnlyChecksConfiguredNumberOfPreviousPasswords()
    {
        var userId = "user1";
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "test@example.com"
        };

        var limitedOptions = Options.Create(new PasswordPolicyOptions
        {
            PreviousPasswordCount = 2
        });
        var limitedValidator = new PreviousPasswordValidator(_dbContext, limitedOptions);

        for (var i = 0; i < 5; i++)
        {
            _dbContext.UserPreviousPasswords.Add(new UserPreviousPassword
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PasswordHash = $"hash{i}",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-i)
            });
        }

        await _dbContext.SaveChangesAsync();

        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(user, "hash0", "newPassword"))
            .Returns(PasswordVerificationResult.Failed);
        _passwordHasherMock
            .Setup(x => x.VerifyHashedPassword(user, "hash1", "newPassword"))
            .Returns(PasswordVerificationResult.Failed);

        var result = await limitedValidator.ValidateAsync(_userManagerMock.Object, user, "newPassword");

        Assert.That(result.Succeeded, Is.True);
    }
}