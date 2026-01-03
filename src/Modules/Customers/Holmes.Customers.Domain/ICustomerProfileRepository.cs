namespace Holmes.Customers.Domain;

/// <summary>
///     Contact information for customer profile creation.
/// </summary>
public sealed record CustomerContactInfo(
    string Name,
    string Email,
    string? Phone,
    string? Role
);

/// <summary>
///     Repository for customer profile persistence.
/// </summary>
public interface ICustomerProfileRepository
{
    /// <summary>
    ///     Creates a customer profile with optional contacts.
    /// </summary>
    Task CreateProfileAsync(
        string customerId,
        string? policySnapshotId,
        string? billingEmail,
        IReadOnlyCollection<CustomerContactInfo>? contacts,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken
    );
}