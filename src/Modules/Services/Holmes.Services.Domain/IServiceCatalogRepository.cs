namespace Holmes.Services.Domain;

/// <summary>
///     Repository for service catalog snapshot persistence.
/// </summary>
public interface IServiceCatalogRepository
{
    /// <summary>
    ///     Saves a new catalog configuration snapshot.
    /// </summary>
    Task SaveSnapshotAsync(
        string customerId,
        int version,
        string configJson,
        string createdBy,
        CancellationToken cancellationToken
    );
}