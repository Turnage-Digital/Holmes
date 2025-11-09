using Holmes.Core.Infrastructure.Sql;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Subjects.Infrastructure.Sql;

public sealed class SubjectsUnitOfWork : UnitOfWork<SubjectsDbContext>, ISubjectsUnitOfWork
{
    private readonly Lazy<ISubjectRepository> _subjects;

    public SubjectsUnitOfWork(SubjectsDbContext dbContext, IMediator mediator)
        : base(dbContext, mediator)
    {
        _subjects = new Lazy<ISubjectRepository>(() => new SqlSubjectRepository(dbContext));
    }

    public ISubjectRepository Subjects => _subjects.Value;
}
