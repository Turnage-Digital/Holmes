using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Users.Application.Abstractions;

/// <summary>
///     Query interface for user access checks. Used by security infrastructure
///     to determine user permissions without direct DbContext dependency.
/// </summary>
public interface IUserAccessQueries
{
    /// <summary>
    ///     Checks if the specified user has global admin privileges.
    /// </summary>
    Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the global (non-customer-scoped) role names for the specified user.
    /// </summary>
    Task<IReadOnlyList<string>> GetGlobalRolesAsync(UlidId userId, CancellationToken cancellationToken);
}