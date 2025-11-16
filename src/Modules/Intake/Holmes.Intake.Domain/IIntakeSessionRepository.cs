using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Intake.Domain;

public interface IIntakeSessionRepository
{
    Task<IntakeSession?> GetByIdAsync(UlidId id, CancellationToken cancellationToken);
    Task AddAsync(IntakeSession session, CancellationToken cancellationToken);
    Task UpdateAsync(IntakeSession session, CancellationToken cancellationToken);
}