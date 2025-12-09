namespace Holmes.Core.Application.Abstractions;

/// <summary>
/// Provides access to the current tenant and actor context.
/// Used for event persistence and audit trails.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The current tenant identifier, or "*" for global/system operations.
    /// </summary>
    string TenantId { get; }

    /// <summary>
    /// The current actor (user) identifier, if authenticated.
    /// </summary>
    string? ActorId { get; }
}
