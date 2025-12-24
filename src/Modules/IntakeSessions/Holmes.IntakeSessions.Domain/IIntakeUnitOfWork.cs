using Holmes.Core.Domain;

namespace Holmes.IntakeSessions.Domain;

public interface IIntakeUnitOfWork : IUnitOfWork
{
    IIntakeSessionRepository IntakeSessions { get; }
}