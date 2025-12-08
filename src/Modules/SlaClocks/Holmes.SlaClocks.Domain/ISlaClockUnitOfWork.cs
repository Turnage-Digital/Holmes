using Holmes.Core.Domain;

namespace Holmes.SlaClocks.Domain;

public interface ISlaClockUnitOfWork : IUnitOfWork
{
    ISlaClockRepository SlaClocks { get; }
}