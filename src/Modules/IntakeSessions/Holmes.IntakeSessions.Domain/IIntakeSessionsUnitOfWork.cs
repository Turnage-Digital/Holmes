using Holmes.Core.Domain;

namespace Holmes.IntakeSessions.Domain;

public interface IIntakeSessionsUnitOfWork : IUnitOfWork
{
    IIntakeSessionRepository IntakeSessions { get; }
}
