using Holmes.Core.Contracts;
using Holmes.Core.Contracts.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Infrastructure.Sql;

public sealed class UsersUnitOfWork(
    UsersDbContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<UsersDbContext>(dbContext, mediator, eventStore, serializer, tenantContext), IUsersUnitOfWork
{
    private readonly Lazy<IUserRepository> _users = new(() => new UserRepository(dbContext));

    public IUserRepository Users => _users.Value;
}