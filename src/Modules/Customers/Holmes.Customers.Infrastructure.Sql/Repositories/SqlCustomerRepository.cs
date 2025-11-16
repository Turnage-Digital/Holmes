using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql.Repositories;

public class SqlCustomerRepository(CustomersDbContext dbContext)
    : ICustomerRepository
{
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        var entity = ToDb(customer);
        dbContext.Customers.Add(entity);
        UpsertDirectory(entity);
        await Task.CompletedTask;
    }

    public async Task<Customer?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Customers
            .Include(c => c.Admins)
            .FirstOrDefaultAsync(c => c.CustomerId == id.ToString(), cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return Rehydrate(entity);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Customers
            .Include(c => c.Admins)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id.ToString(), cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Customer '{customer.Id}' not found.");
        }

        ApplyState(customer, entity);
        UpsertDirectory(entity);
    }

    private static Customer Rehydrate(CustomerDb entity)
    {
        return Customer.Rehydrate(
            UlidId.Parse(entity.CustomerId),
            entity.Name,
            entity.Status,
            entity.CreatedAt,
            entity.Admins
                .Select(a => new CustomerAdmin(UlidId.Parse(a.UserId), a.AssignedBy, a.AssignedAt)));
    }

    private static void ApplyState(Customer customer, CustomerDb entity)
    {
        entity.Name = customer.Name;
        entity.Status = customer.Status;

        var desiredAdmins = customer.Admins.ToDictionary(a => a.UserId.ToString(), a => a);

        foreach (var existing in entity.Admins.Where(a => !desiredAdmins.ContainsKey(a.UserId)).ToList())
        {
            entity.Admins.Remove(existing);
        }

        foreach (var admin in desiredAdmins.Values)
        {
            var existing = entity.Admins.FirstOrDefault(a => a.UserId == admin.UserId.ToString());
            if (existing is null)
            {
                entity.Admins.Add(new CustomerAdminDb
                {
                    CustomerId = entity.CustomerId,
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

    private static CustomerDb ToDb(Customer customer)
    {
        var entity = new CustomerDb
        {
            CustomerId = customer.Id.ToString(),
            Name = customer.Name,
            Status = customer.Status,
            CreatedAt = customer.CreatedAt
        };

        foreach (var admin in customer.Admins)
        {
            entity.Admins.Add(new CustomerAdminDb
            {
                CustomerId = entity.CustomerId,
                UserId = admin.UserId.ToString(),
                AssignedBy = admin.AssignedBy,
                AssignedAt = admin.AssignedAt
            });
        }

        return entity;
    }

    private void UpsertDirectory(CustomerDb entity)
    {
        var directory = dbContext.CustomerDirectory
            .SingleOrDefault(c => c.CustomerId == entity.CustomerId);

        if (directory is null)
        {
            directory = new CustomerDirectoryDb
            {
                CustomerId = entity.CustomerId,
                Name = entity.Name,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                AdminCount = entity.Admins.Count
            };
            dbContext.CustomerDirectory.Add(directory);
        }
        else
        {
            directory.Name = entity.Name;
            directory.Status = entity.Status;
            directory.AdminCount = entity.Admins.Count;
        }
    }
}
