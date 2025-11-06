using Holmes.Core.Domain;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Infrastructure.Sql;

public sealed class UsersUnitOfWork : UnitOfWork<UsersDbContext>, IUsersUnitOfWork
{
    public UsersUnitOfWork(UsersDbContext dbContext, IMediator mediator)
        : base(dbContext, mediator)
    {
    }

    public void RegisterDomainEvents(IHasDomainEvents aggregate)
    {
        if (aggregate is null)
        {
            return;
        }

        CollectDomainEvents(aggregate);
    }

    public void RegisterDomainEvents(IEnumerable<IHasDomainEvents> aggregates)
    {
        if (aggregates is null)
        {
            return;
        }

        CollectDomainEvents(aggregates);
    }
}