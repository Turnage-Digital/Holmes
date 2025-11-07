using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Users.Domain;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UlidId id, CancellationToken cancellationToken);

    Task<User?> GetByExternalIdentityAsync(string issuer, string subject, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);
}