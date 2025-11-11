using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Core.Application;

public interface ICurrentUserInitializer
{
    Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken);
}