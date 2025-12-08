using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Infrastructure.Sql.Entities;

namespace Holmes.Customers.Infrastructure.Sql.Mappers;

public static class CustomerMapper
{
    public static CustomerListItemDto ToListItem(
        CustomerDirectoryDb directory,
        CustomerProfileDb? profile,
        IReadOnlyCollection<CustomerContactDb> contacts
    )
    {
        var policySnapshotId = string.IsNullOrWhiteSpace(profile?.PolicySnapshotId)
            ? "policy-default"
            : profile!.PolicySnapshotId;

        var billingEmail = string.IsNullOrWhiteSpace(profile?.BillingEmail) ? null : profile!.BillingEmail;

        var contactResponses = contacts
            .OrderBy(c => c.Name)
            .Select(c => new CustomerContactDto(
                c.ContactId,
                c.Name,
                c.Email,
                c.Phone,
                c.Role))
            .ToList();

        return new CustomerListItemDto(
            directory.CustomerId,
            profile?.TenantId ?? directory.CustomerId,
            directory.Name,
            directory.Status,
            policySnapshotId,
            billingEmail,
            contactResponses,
            directory.CreatedAt,
            profile?.UpdatedAt ?? directory.CreatedAt);
    }
}