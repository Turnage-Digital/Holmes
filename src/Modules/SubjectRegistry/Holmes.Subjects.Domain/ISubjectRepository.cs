using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Domain;

public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(UlidId id, CancellationToken cancellationToken);

    Task AddAsync(Subject subject, CancellationToken cancellationToken);

    Task UpdateAsync(Subject subject, CancellationToken cancellationToken);
}