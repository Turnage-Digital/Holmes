using Holmes.Users.Domain;

namespace Holmes.Users.Application.Abstractions.Projections;

/// <summary>
///     Writes user projection data for read model queries.
///     Called by event handlers to keep projections in sync.
/// </summary>
public interface IUserProjectionWriter
{
    Task UpsertAsync(UserProjectionModel model, CancellationToken cancellationToken);

    Task UpdateStatusAsync(string userId, UserStatus status, CancellationToken cancellationToken);

    Task UpdateProfileAsync(string userId, string email, string? displayName, CancellationToken cancellationToken);
}

/// <summary>
///     Model representing the user projection data.
/// </summary>
public sealed record UserProjectionModel(
    string UserId,
    string Email,
    string? DisplayName,
    string Issuer,
    string Subject,
    DateTimeOffset LastAuthenticatedAt,
    UserStatus Status
);