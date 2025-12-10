using Holmes.Customers.Domain;

namespace Holmes.Customers.Application.Abstractions.Projections;

/// <summary>
///     Writes customer projection data for read model queries.
///     Called by event handlers to keep projections in sync.
/// </summary>
public interface ICustomerProjectionWriter
{
    Task UpsertAsync(CustomerProjectionModel model, CancellationToken cancellationToken);

    Task UpdateStatusAsync(string customerId, CustomerStatus status, CancellationToken cancellationToken);

    Task UpdateAdminCountAsync(string customerId, int delta, CancellationToken cancellationToken);

    Task UpdateNameAsync(string customerId, string name, CancellationToken cancellationToken);
}

/// <summary>
///     Model representing the customer projection data.
/// </summary>
public sealed record CustomerProjectionModel(
    string CustomerId,
    string Name,
    CustomerStatus Status,
    DateTimeOffset CreatedAt,
    int AdminCount
);