using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Subjects.Domain;
using MediatR;

namespace Holmes.Subjects.Infrastructure.Sql;

public sealed class SubjectsUnitOfWork(
    SubjectsDbContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<SubjectsDbContext>(dbContext, mediator, eventStore, serializer, tenantContext), ISubjectsUnitOfWork
{
    private readonly Lazy<ISubjectRepository> _subjects = new(() => new SubjectRepository(dbContext));

    public ISubjectRepository Subjects => _subjects.Value;
}