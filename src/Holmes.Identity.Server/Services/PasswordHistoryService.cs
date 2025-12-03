using Holmes.Identity.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Holmes.Identity.Server.Services;

public interface IPasswordHistoryService
{
    Task RecordPasswordChangeAsync(
        string userId,
        string oldPasswordHash,
        CancellationToken cancellationToken = default
    );

    Task CleanupOldPasswordsAsync(string userId, CancellationToken cancellationToken = default);
}

public class PasswordHistoryService(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<PasswordPolicyOptions> options
) : IPasswordHistoryService
{
    public async Task RecordPasswordChangeAsync(
        string userId,
        string oldPasswordHash,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(oldPasswordHash))
        {
            return;
        }

        var previousPassword = new UserPreviousPassword
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PasswordHash = oldPasswordHash,
            CreatedAt = timeProvider.GetUtcNow()
        };

        dbContext.UserPreviousPasswords.Add(previousPassword);
        await dbContext.SaveChangesAsync(cancellationToken);

        await CleanupOldPasswordsAsync(userId, cancellationToken);
    }

    public async Task CleanupOldPasswordsAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var keepCount = options.Value.PreviousPasswordCount;

        var passwordsToDelete = await dbContext.UserPreviousPasswords
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(keepCount)
            .ToListAsync(cancellationToken);

        if (passwordsToDelete.Count > 0)
        {
            dbContext.UserPreviousPasswords.RemoveRange(passwordsToDelete);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}