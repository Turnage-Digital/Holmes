using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Abstractions.Dtos;

namespace Holmes.IntakeSessions.Application.Abstractions;

/// <summary>
///     Query interface for intake session lookups. Used by application layer for read operations.
/// </summary>
public interface IIntakeSessionQueries
{
    /// <summary>
    ///     Gets intake session bootstrap data by ID and resume token.
    ///     Returns null if session not found or resume token is invalid.
    /// </summary>
    Task<IntakeSessionBootstrapDto?> GetBootstrapAsync(
        UlidId intakeSessionId,
        string resumeToken,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets intake session summary by ID.
    /// </summary>
    Task<IntakeSessionSummaryDto?> GetByIdAsync(
        string intakeSessionId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets intake sessions for an order.
    /// </summary>
    Task<IReadOnlyList<IntakeSessionSummaryDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets the active (non-expired, non-superseded) session for an order.
    /// </summary>
    Task<IntakeSessionSummaryDto?> GetActiveByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Checks if a session exists.
    /// </summary>
    Task<bool> ExistsAsync(string intakeSessionId, CancellationToken cancellationToken);
}