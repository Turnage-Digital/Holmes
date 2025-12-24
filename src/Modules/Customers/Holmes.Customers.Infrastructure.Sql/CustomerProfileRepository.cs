using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql;

public sealed class CustomerProfileRepository(CustomersDbContext dbContext) : ICustomerProfileRepository
{
    public async Task<bool> CreateProfileAsync(
        string customerId,
        string? policySnapshotId,
        string? billingEmail,
        IReadOnlyCollection<CustomerContactInfo>? contacts,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken
    )
    {
        var exists = await dbContext.CustomerProfiles
            .AnyAsync(p => p.CustomerId == customerId, cancellationToken);

        if (exists)
        {
            return false; // Profile already exists
        }

        var profile = new CustomerProfileDb
        {
            CustomerId = customerId,
            TenantId = Ulid.NewUlid().ToString(),
            PolicySnapshotId = string.IsNullOrWhiteSpace(policySnapshotId)
                ? "policy-default"
                : policySnapshotId.Trim(),
            BillingEmail = string.IsNullOrWhiteSpace(billingEmail)
                ? null
                : billingEmail.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        var contactEntities = (contacts ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Email))
            .Select(c => new CustomerContactDb
            {
                ContactId = Ulid.NewUlid().ToString(),
                CustomerId = customerId,
                Name = c.Name.Trim(),
                Email = c.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(c.Phone) ? null : c.Phone.Trim(),
                Role = string.IsNullOrWhiteSpace(c.Role) ? null : c.Role.Trim(),
                CreatedAt = createdAt
            })
            .ToList();

        dbContext.CustomerProfiles.Add(profile);
        if (contactEntities.Count > 0)
        {
            await dbContext.CustomerContacts.AddRangeAsync(contactEntities, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}