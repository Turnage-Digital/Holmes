using Holmes.Core.Domain;

namespace Holmes.SlaClocks.Domain;

public interface ISlaClocksUnitOfWork : IUnitOfWork
{
    ISlaClockRepository SlaClocks { get; }
}
