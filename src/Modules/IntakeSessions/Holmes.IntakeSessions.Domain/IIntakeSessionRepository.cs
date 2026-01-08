using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Domain;

public interface IIntakeSessionRepository
{
    Task<IntakeSession?> GetByIdAsync(UlidId id, CancellationToken cancellationToken);
    Task<IntakeSession?> GetByOrderIdAsync(UlidId orderId, CancellationToken cancellationToken);
    Task AddAsync(IntakeSession session, CancellationToken cancellationToken);
    Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken);
}
