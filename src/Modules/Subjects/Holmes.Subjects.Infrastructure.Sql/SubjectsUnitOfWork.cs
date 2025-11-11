using Holmes.Core.Infrastructure.Sql;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Subjects.Infrastructure.Sql;

public sealed class SubjectsUnitOfWork(SubjectsDbContext dbContext, IMediator mediator)
    : UnitOfWork<SubjectsDbContext>(dbContext, mediator), ISubjectsUnitOfWork
{
    private readonly Lazy<ISubjectRepository> _subjects = new(() => new SqlSubjectRepository(dbContext));

    public ISubjectRepository Subjects => _subjects.Value;
}