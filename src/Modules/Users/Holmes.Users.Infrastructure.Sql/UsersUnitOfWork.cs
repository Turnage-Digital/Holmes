using Holmes.Core.Infrastructure.Sql;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Users.Infrastructure.Sql;

public sealed class UsersUnitOfWork(UsersDbContext dbContext, IMediator mediator)
    : UnitOfWork<UsersDbContext>(dbContext, mediator), IUsersUnitOfWork
{
    private readonly Lazy<IUserRepository> _users = new(() => new SqlUserRepository(dbContext));

    public IUserRepository Users => _users.Value;
}