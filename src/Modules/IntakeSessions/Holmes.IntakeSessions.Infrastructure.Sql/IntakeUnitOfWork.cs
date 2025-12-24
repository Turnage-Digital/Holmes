using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.IntakeSessions.Domain;
using MediatR;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

public sealed class IntakeUnitOfWork(
    IntakeDbContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<IntakeDbContext>(dbContext, mediator, eventStore, serializer, tenantContext), IIntakeUnitOfWork
{
    private readonly Lazy<IIntakeSessionRepository> _sessions = new(() => new IntakeSessionRepository(dbContext));

    public IIntakeSessionRepository IntakeSessions => _sessions.Value;
}