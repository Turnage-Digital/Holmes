using Holmes.Core.Infrastructure.Sql;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Infrastructure.Sql;

public sealed class SlaClockUnitOfWork(
    SlaClockDbContext context,
    IMediator mediator,
    ISlaClockRepository slaClockRepository
)
    : UnitOfWork<SlaClockDbContext>(context, mediator), ISlaClockUnitOfWork
{
    public ISlaClockRepository SlaClocks => slaClockRepository;
}