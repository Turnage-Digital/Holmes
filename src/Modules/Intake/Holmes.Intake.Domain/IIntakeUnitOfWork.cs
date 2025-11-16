using Holmes.Core.Domain;

namespace Holmes.Intake.Domain;

public interface IIntakeUnitOfWork : IUnitOfWork
{
    IIntakeSessionRepository IntakeSessions { get; }
}