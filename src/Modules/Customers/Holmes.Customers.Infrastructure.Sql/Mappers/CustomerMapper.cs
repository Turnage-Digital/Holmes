using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Entities;

namespace Holmes.Customers.Infrastructure.Sql.Mappers;

public static class CustomerMapper
{
    public static Customer ToDomain(CustomerDb db)
    {
        return Customer.Rehydrate(
            UlidId.Parse(db.CustomerId),
            db.Name,
            db.Status,
            db.CreatedAt,
            db.Admins.Select(a => new CustomerAdmin(UlidId.Parse(a.UserId), a.AssignedBy, a.AssignedAt)));
    }

    public static CustomerDb ToDb(Customer customer)
    {
        var db = new CustomerDb
        {
            CustomerId = customer.Id.ToString(),
            Name = customer.Name,
            Status = customer.Status,
            CreatedAt = customer.CreatedAt
        };

        foreach (var admin in customer.Admins)
        {
            db.Admins.Add(new CustomerAdminDb
            {
                CustomerId = db.CustomerId,
                UserId = admin.UserId.ToString(),
                AssignedBy = admin.AssignedBy,
                AssignedAt = admin.AssignedAt
            });
        }

        return db;
    }

    public static void UpdateDb(CustomerDb db, Customer customer)
    {
        db.Name = customer.Name;
        db.Status = customer.Status;

        SyncAdmins(db, customer);
    }

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

    private static void SyncAdmins(CustomerDb db, Customer customer)
    {
        var desiredAdmins = customer.Admins.ToDictionary(a => a.UserId.ToString(), a => a);

        foreach (var existing in db.Admins.Where(a => !desiredAdmins.ContainsKey(a.UserId)).ToList())
        {
            db.Admins.Remove(existing);
        }

        foreach (var admin in desiredAdmins.Values)
        {
            var existing = db.Admins.FirstOrDefault(a => a.UserId == admin.UserId.ToString());
            if (existing is null)
            {
                db.Admins.Add(new CustomerAdminDb
                {
                    CustomerId = db.CustomerId,
                    UserId = admin.UserId.ToString(),
                    AssignedAt = admin.AssignedAt,
                    AssignedBy = admin.AssignedBy
                });
            }
            else
            {
                existing.AssignedAt = admin.AssignedAt;
                existing.AssignedBy = admin.AssignedBy;
            }
        }
    }
}
