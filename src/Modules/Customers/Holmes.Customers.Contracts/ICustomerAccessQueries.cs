using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Contracts;

/// <summary>
///     Query interface for customer access checks. Used by security infrastructure
///     to determine which customers a user can access without direct DbContext dependency.
/// </summary>
public interface ICustomerAccessQueries
{
    /// <summary>
    ///     Gets the IDs of all customers where the specified user is an admin.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetAdminCustomerIdsAsync(UlidId userId, CancellationToken cancellationToken);
}