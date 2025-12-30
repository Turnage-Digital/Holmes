using Holmes.Users.Contracts;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Users.Infrastructure.Sql;

public sealed class UserProjectionWriter(
    UsersDbContext dbContext,
    ILogger<UserProjectionWriter> logger
) : IUserProjectionWriter
{
    public async Task UpsertAsync(UserProjectionModel model, CancellationToken cancellationToken)
    {
        // Look up by UserId first, then by Issuer+Subject (unique constraint)
        // This handles idempotent re-delivery of events in the outbox pattern
        var record = await dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.UserId == model.UserId, cancellationToken);

        record ??= await dbContext.UserProjections
            .FirstOrDefaultAsync(
                x => x.Issuer == model.Issuer && x.Subject == model.Subject,
                cancellationToken);

        if (record is null)
        {
            record = new UserProjectionDb
            {
                UserId = model.UserId
            };
            dbContext.UserProjections.Add(record);
        }

        record.Email = model.Email;
        record.DisplayName = model.DisplayName;
        record.Issuer = model.Issuer;
        record.Subject = model.Subject;
        record.LastAuthenticatedAt = model.LastAuthenticatedAt;
        record.Status = model.Status;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(string userId, UserStatus status, CancellationToken cancellationToken)
    {
        var record = await dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("User projection not found for status update: {UserId}", userId);
            return;
        }

        record.Status = status;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateProfileAsync(
        string userId,
        string email,
        string? displayName,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("User projection not found for profile update: {UserId}", userId);
            return;
        }

        record.Email = email;
        record.DisplayName = displayName;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}