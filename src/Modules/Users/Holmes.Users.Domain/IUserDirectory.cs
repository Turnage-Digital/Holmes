using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Users.Domain;

public interface IUserDirectory
{
    Task<bool> ExistsAsync(UlidId userId, CancellationToken cancellationToken);
}