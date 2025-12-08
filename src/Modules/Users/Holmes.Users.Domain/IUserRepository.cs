using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Users.Domain;

/// <summary>
///     Write-focused repository for User aggregate.
///     Query methods have been moved to IUserQueries in Application.Abstractions.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UlidId id, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);
}