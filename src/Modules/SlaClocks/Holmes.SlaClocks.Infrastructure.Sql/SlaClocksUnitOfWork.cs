using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Infrastructure.Sql;

public sealed class SlaClocksUnitOfWork(
    SlaClocksDbContext context,
    IMediator mediator,
    ISlaClockRepository slaClockRepository,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<SlaClocksDbContext>(context, mediator, eventStore, serializer, tenantContext), ISlaClocksUnitOfWork
{
    public ISlaClockRepository SlaClocks => slaClockRepository;
}