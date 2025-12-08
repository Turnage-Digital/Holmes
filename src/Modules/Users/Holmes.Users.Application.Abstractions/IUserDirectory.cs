using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Users.Application.Abstractions;

/// <summary>
///     Read-only directory for checking user existence across module boundaries.
/// </summary>
public interface IUserDirectory
{
    Task<bool> ExistsAsync(UlidId userId, CancellationToken cancellationToken);
}
