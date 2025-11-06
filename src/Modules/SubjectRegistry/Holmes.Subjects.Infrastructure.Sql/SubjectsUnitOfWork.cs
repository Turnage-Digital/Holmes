using Holmes.Core.Domain;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Infrastructure.Sql;

public sealed class SubjectsUnitOfWork : UnitOfWork<SubjectsDbContext>, ISubjectsUnitOfWork
{
    public SubjectsUnitOfWork(SubjectsDbContext dbContext, IMediator mediator)
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