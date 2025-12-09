using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Application.Abstractions.Queries;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Customers.Infrastructure.Sql.Mappers;
using Holmes.Customers.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql.Queries;

public sealed class SqlCustomerQueries(CustomersDbContext dbContext) : ICustomerQueries
{
    public async Task<CustomerPagedResult> GetCustomersPagedAsync(
        IReadOnlyCollection<string>? allowedCustomerIds,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var listingSpec = new CustomersVisibleToUserSpecification(allowedCustomerIds, page, pageSize);
        var countSpec = new CustomersVisibleToUserSpecification(allowedCustomerIds);

        var totalItems = await dbContext.CustomerProjections
            .AsNoTracking()
            .ApplySpecification(countSpec)
            .CountAsync(cancellationToken);

        var directories = await dbContext.CustomerProjections
            .AsNoTracking()
            .ApplySpecification(listingSpec)
            .ToListAsync(cancellationToken);

        var customerIdsPage = directories.Select(c => c.CustomerId).ToList();

        var profiles = await dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(p => customerIdsPage.Contains(p.CustomerId))
            .ToDictionaryAsync(p => p.CustomerId, cancellationToken);

        var contacts = await dbContext.CustomerContacts
            .AsNoTracking()
            .Where(c => customerIdsPage.Contains(c.CustomerId))
            .GroupBy(c => c.CustomerId)
            .ToDictionaryAsync(
                g => g.Key,
                IReadOnlyCollection<CustomerContactDb> (g) => g.ToList(),
                cancellationToken);

        var items = directories
            .Select(directory =>
            {
                profiles.TryGetValue(directory.CustomerId, out var profile);
                contacts.TryGetValue(directory.CustomerId, out var contactList);
                return CustomerMapper.ToListItem(directory, profile, contactList ?? []);
            })
            .ToList();

        return new CustomerPagedResult(items, totalItems);
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        var listItem = await GetListItemByIdAsync(customerId, cancellationToken);
        if (listItem is null)
        {
            return null;
        }

        var admins = await GetAdminsAsync(customerId, cancellationToken);

        return new CustomerDetailDto(
            listItem.Id,
            listItem.TenantId,
            listItem.Name,
            listItem.Status,
            listItem.PolicySnapshotId,
            listItem.BillingEmail,
            listItem.CreatedAt,
            listItem.UpdatedAt,
            listItem.Contacts,
            admins);
    }

    public async Task<CustomerListItemDto?> GetListItemByIdAsync(string customerId, CancellationToken cancellationToken)
    {
        var directory = await dbContext.CustomerProjections.AsNoTracking()
            .SingleOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (directory is null)
        {
            return null;
        }

        var profile = await dbContext.CustomerProfiles.AsNoTracking()
            .Include(p => p.Contacts)
            .SingleOrDefaultAsync(p => p.CustomerId == customerId, cancellationToken);

        var contactList = profile?.Contacts.ToList() ?? [];
        return CustomerMapper.ToListItem(directory, profile, contactList);
    }

    public async Task<bool> ExistsAsync(string customerId, CancellationToken cancellationToken)
    {
        return await dbContext.CustomerProjections
            .AsNoTracking()
            .AnyAsync(c => c.CustomerId == customerId, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerAdminDto>> GetAdminsAsync(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        return await dbContext.CustomerAdmins.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Select(a => new CustomerAdminDto(a.UserId, a.AssignedBy.ToString(), a.AssignedAt))
            .ToListAsync(cancellationToken);
    }
}