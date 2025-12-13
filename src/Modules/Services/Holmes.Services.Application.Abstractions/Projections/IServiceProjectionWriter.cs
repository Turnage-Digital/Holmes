using Holmes.Services.Domain;

namespace Holmes.Services.Application.Abstractions.Projections;

/// <summary>
///     Writes service request projection data for read model queries.
///     Called by event handlers to keep projections in sync.
/// </summary>
public interface IServiceProjectionWriter
{
    /// <summary>
    ///     Inserts or updates a full service request projection record.
    ///     Called on ServiceRequestCreated events.
    /// </summary>
    Task UpsertAsync(ServiceProjectionModel model, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates the status to Dispatched. Called on ServiceRequestDispatched events.
    /// </summary>
    Task UpdateDispatchedAsync(
        string serviceRequestId,
        string vendorCode,
        string? vendorReferenceId,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates the status to InProgress. Called on ServiceRequestInProgress events.
    /// </summary>
    Task UpdateInProgressAsync(
        string serviceRequestId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates the status to Completed. Called on ServiceRequestCompleted events.
    /// </summary>
    Task UpdateCompletedAsync(
        string serviceRequestId,
        ServiceResultStatus resultStatus,
        int recordCount,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates the status to Failed. Called on ServiceRequestFailed events.
    /// </summary>
    Task UpdateFailedAsync(
        string serviceRequestId,
        string errorMessage,
        int attemptCount,
        bool willRetry,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates the status to Canceled. Called on ServiceRequestCanceled events.
    /// </summary>
    Task UpdateCanceledAsync(
        string serviceRequestId,
        string reason,
        DateTimeOffset canceledAt,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates attempt info after retry. Called on ServiceRequestRetried events.
    /// </summary>
    Task UpdateRetriedAsync(
        string serviceRequestId,
        int attemptCount,
        DateTimeOffset retriedAt,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     Model representing the full service request projection data.
/// </summary>
public sealed record ServiceProjectionModel(
    string ServiceRequestId,
    string OrderId,
    string CustomerId,
    string ServiceTypeCode,
    ServiceCategory Category,
    ServiceStatus Status,
    int Tier,
    string? ScopeType,
    string? ScopeValue,
    DateTimeOffset CreatedAt
);