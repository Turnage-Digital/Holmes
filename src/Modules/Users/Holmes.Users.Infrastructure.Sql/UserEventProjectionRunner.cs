using Holmes.Core.Contracts.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Users.Infrastructure.Sql;

/// <summary>
///     Event-based projection runner for User projections.
///     Replays User domain events to rebuild the user_projections table.
/// </summary>
public sealed class UserEventProjectionRunner(
    UsersDbContext usersDbContext,
    CoreDbContext coreDbContext,
    IEventStore eventStore,
    IDomainEventSerializer serializer,
    IPublisher publisher,
    ILogger<UserEventProjectionRunner> logger
)
    : EventProjectionRunner(coreDbContext, eventStore, serializer, publisher, logger)
{
    protected override string ProjectionName => "users.user_projection.events";

    protected override string[]? StreamTypes => ["User"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (usersDbContext.Database.IsRelational())
        {
            await usersDbContext.UserProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            usersDbContext.UserProjections.RemoveRange(usersDbContext.UserProjections);
            await usersDbContext.SaveChangesAsync(cancellationToken);
        }

        usersDbContext.ChangeTracker.Clear();
    }
}