using Holmes.Customers.Contracts.Dtos;

namespace Holmes.Customers.Contracts;

/// <summary>
///     Query interface for customer lookups. Used by application layer for read operations.
/// </summary>
public interface ICustomerQueries
{
    /// <summary>
    ///     Gets a paginated list of customers visible to the user.
    /// </summary>
    Task<CustomerPagedResult> GetCustomersPagedAsync(
        IReadOnlyCollection<string>? allowedCustomerIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets a customer by ID with full details including admins.
    /// </summary>
    Task<CustomerDetailDto?> GetByIdAsync(string customerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets a customer list item by ID (less detail than full GetById).
    /// </summary>
    Task<CustomerListItemDto?> GetListItemByIdAsync(string customerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Checks if a customer exists.
    /// </summary>
    Task<bool> ExistsAsync(string customerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets the admin list for a customer.
    /// </summary>
    Task<IReadOnlyList<CustomerAdminDto>> GetAdminsAsync(string customerId, CancellationToken cancellationToken);
}

/// <summary>
///     Paginated result for customer queries.
/// </summary>
public sealed record CustomerPagedResult(
    IReadOnlyList<CustomerListItemDto> Items,
    int TotalCount
);