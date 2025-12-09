using Holmes.Users.Application.Abstractions.Projections;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql.Projections;

public sealed class SqlUserProjectionWriter(UsersDbContext dbContext) : IUserProjectionWriter
{
    public async Task UpsertAsync(UserProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.UserProjections
            .FirstOrDefaultAsync(x => x.UserId == model.UserId, cancellationToken);

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

        if (record is not null)
        {
            record.Status = status;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
