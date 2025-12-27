using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.IntakeSessions.Domain;
using MediatR;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

public sealed class IntakeSessionsUnitOfWork(
    IntakeSessionsDbContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<IntakeSessionsDbContext>(dbContext, mediator, eventStore, serializer, tenantContext),
        IIntakeSessionsUnitOfWork
{
    private readonly Lazy<IIntakeSessionRepository> _sessions = new(() => new IntakeSessionRepository(dbContext));

    public IIntakeSessionRepository IntakeSessions => _sessions.Value;
}