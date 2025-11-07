using System;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Users.Infrastructure.Sql;

public sealed class UsersUnitOfWork : UnitOfWork<UsersDbContext>, IUsersUnitOfWork
{
    private readonly Lazy<IUserRepository> _users;
    private readonly Lazy<IUserDirectory> _directory;

    public UsersUnitOfWork(UsersDbContext dbContext, IMediator mediator)
        : base(dbContext, mediator)
    {
        _users = new Lazy<IUserRepository>(() => new SqlUserRepository(dbContext, this));
        _directory = new Lazy<IUserDirectory>(() => new SqlUserDirectory(dbContext));
    }

    public IUserRepository Users => _users.Value;

    public IUserDirectory UserDirectory => _directory.Value;
}
