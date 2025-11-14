using Holmes.Core.Infrastructure.Sql;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql.Repositories;
using MediatR;

namespace Holmes.Intake.Infrastructure.Sql;

public sealed class IntakeUnitOfWork(IntakeDbContext dbContext, IMediator mediator)
    : UnitOfWork<IntakeDbContext>(dbContext, mediator), IIntakeUnitOfWork
{
    private readonly Lazy<IIntakeSessionRepository> _sessions = new(() => new SqlIntakeSessionRepository(dbContext));

    public IIntakeSessionRepository IntakeSessions => _sessions.Value;
}