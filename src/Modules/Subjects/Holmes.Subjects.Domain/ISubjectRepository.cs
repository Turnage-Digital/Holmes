using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Domain;

public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(UlidId id, CancellationToken cancellationToken);

    /// <summary>
    ///     Finds a subject by their email address.
    ///     Used to reuse existing subjects across orders.
    /// </summary>
    Task<Subject?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(Subject subject, CancellationToken cancellationToken);

    Task UpdateAsync(Subject subject, CancellationToken cancellationToken);
}