using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Users.Infrastructure.Sql.Projections;

/// <summary>
///     Event-based projection runner for User projections.
///     Replays User domain events to rebuild the user_projections table.
/// </summary>
public sealed class UserEventProjectionRunner : EventProjectionRunner
{
    private readonly UsersDbContext _usersDbContext;

    public UserEventProjectionRunner(
        UsersDbContext usersDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<UserEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _usersDbContext = usersDbContext;
    }

    protected override string ProjectionName => "users.user_projection.events";

    protected override string[]? StreamTypes => ["User"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_usersDbContext.Database.IsRelational())
        {
            await _usersDbContext.UserProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _usersDbContext.UserProjections.RemoveRange(_usersDbContext.UserProjections);
            await _usersDbContext.SaveChangesAsync(cancellationToken);
        }

        _usersDbContext.ChangeTracker.Clear();
    }
}