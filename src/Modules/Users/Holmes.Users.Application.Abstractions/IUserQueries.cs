using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Abstractions.Dtos;

namespace Holmes.Users.Application.Abstractions;

/// <summary>
///     Query interface for user lookups. Used by application layer for read operations.
/// </summary>
public interface IUserQueries
{
    /// <summary>
    ///     Gets a user by their external identity (issuer + subject).
    /// </summary>
    Task<UserDto?> GetByExternalIdentityAsync(string issuer, string subject, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets a user by their email address.
    /// </summary>
    Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets a user by their internal ID.
    /// </summary>
    Task<UserDto?> GetByIdAsync(UlidId userId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets all users (for admin listing).
    /// </summary>
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Gets paginated users for admin listing.
    /// </summary>
    Task<UserPagedResult> GetUsersPagedAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets current user details including roles.
    /// </summary>
    Task<CurrentUserDto?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken);
}

/// <summary>
///     Result for paginated user queries.
/// </summary>
public sealed record UserPagedResult(IReadOnlyList<UserDto> Items, int TotalCount);