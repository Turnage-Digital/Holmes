using Holmes.Identity.Server.Data;
using Holmes.Identity.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Holmes.Identity.Server.Tests.Services;

public class PasswordHistoryServiceTests
{
    private ApplicationDbContext _dbContext = null!;
    private IOptions<PasswordPolicyOptions> _options = null!;
    private PasswordHistoryService _service = null!;
    private FakeTimeProvider _timeProvider = null!;

    [SetUp]
    public void SetUp()
    {
        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(dbOptions);

        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero));

        _options = Options.Create(new PasswordPolicyOptions
        {
            ExpirationDays = 90,
            PreviousPasswordCount = 3
        });

        _service = new PasswordHistoryService(_dbContext, _timeProvider, _options);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task RecordPasswordChangeAsync_WithEmptyHash_DoesNothing()
    {
        await _service.RecordPasswordChangeAsync("user1", string.Empty);

        var count = await _dbContext.UserPreviousPasswords.CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task RecordPasswordChangeAsync_WithNullHash_DoesNothing()
    {
        await _service.RecordPasswordChangeAsync("user1", null!);

        var count = await _dbContext.UserPreviousPasswords.CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task RecordPasswordChangeAsync_SavesPasswordHash()
    {
        const string userId = "user1";
        const string hash = "oldPasswordHash";

        await _service.RecordPasswordChangeAsync(userId, hash);

        var saved = await _dbContext.UserPreviousPasswords.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(saved!.UserId, Is.EqualTo(userId));
            Assert.That(saved.PasswordHash, Is.EqualTo(hash));
            Assert.That(saved.CreatedAt, Is.EqualTo(_timeProvider.GetUtcNow()));
        });
    }

    [Test]
    public async Task RecordPasswordChangeAsync_CleansUpOldPasswords()
    {
        const string userId = "user1";

        for (var i = 0; i < 5; i++)
        {
            _dbContext.UserPreviousPasswords.Add(new UserPreviousPassword
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PasswordHash = $"hash{i}",
                CreatedAt = _timeProvider.GetUtcNow().AddDays(-i - 1)
            });
        }

        await _dbContext.SaveChangesAsync();

        await _service.RecordPasswordChangeAsync(userId, "newHash");

        var remaining = await _dbContext.UserPreviousPasswords
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        Assert.That(remaining, Has.Count.EqualTo(3));
        Assert.That(remaining[0].PasswordHash, Is.EqualTo("newHash"));
    }

    [Test]
    public async Task CleanupOldPasswordsAsync_KeepsOnlyConfiguredCount()
    {
        const string userId = "user1";

        for (var i = 0; i < 10; i++)
        {
            _dbContext.UserPreviousPasswords.Add(new UserPreviousPassword
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PasswordHash = $"hash{i}",
                CreatedAt = _timeProvider.GetUtcNow().AddDays(-i)
            });
        }

        await _dbContext.SaveChangesAsync();

        await _service.CleanupOldPasswordsAsync(userId);

        var remaining = await _dbContext.UserPreviousPasswords
            .Where(p => p.UserId == userId)
            .ToListAsync();

        Assert.That(remaining, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task CleanupOldPasswordsAsync_DoesNotAffectOtherUsers()
    {
        const string user1 = "user1";
        const string user2 = "user2";

        for (var i = 0; i < 5; i++)
        {
            _dbContext.UserPreviousPasswords.Add(new UserPreviousPassword
            {
                Id = Guid.NewGuid(),
                UserId = user1,
                PasswordHash = $"hash1_{i}",
                CreatedAt = _timeProvider.GetUtcNow().AddDays(-i)
            });
            _dbContext.UserPreviousPasswords.Add(new UserPreviousPassword
            {
                Id = Guid.NewGuid(),
                UserId = user2,
                PasswordHash = $"hash2_{i}",
                CreatedAt = _timeProvider.GetUtcNow().AddDays(-i)
            });
        }

        await _dbContext.SaveChangesAsync();

        await _service.CleanupOldPasswordsAsync(user1);

        var user1Count = await _dbContext.UserPreviousPasswords.CountAsync(p => p.UserId == user1);
        var user2Count = await _dbContext.UserPreviousPasswords.CountAsync(p => p.UserId == user2);

        Assert.Multiple(() =>
        {
            Assert.That(user1Count, Is.EqualTo(3));
            Assert.That(user2Count, Is.EqualTo(5));
        });
    }
}