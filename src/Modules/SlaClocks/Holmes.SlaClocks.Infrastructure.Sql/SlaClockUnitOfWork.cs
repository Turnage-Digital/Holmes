using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Infrastructure.Sql;

public sealed class SlaClockUnitOfWork(
    SlaClockDbContext context,
    IMediator mediator,
    ISlaClockRepository slaClockRepository,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<SlaClockDbContext>(context, mediator, eventStore, serializer, tenantContext), ISlaClockUnitOfWork
{
    public ISlaClockRepository SlaClocks => slaClockRepository;
}