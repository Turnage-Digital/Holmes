using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Repositories;
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
    private readonly Lazy<IUserRepository> _users = new(() => new SqlUserRepository(dbContext));

    public IUserRepository Users => _users.Value;
}