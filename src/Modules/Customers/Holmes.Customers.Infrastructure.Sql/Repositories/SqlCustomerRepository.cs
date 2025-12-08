using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Customers.Infrastructure.Sql.Mappers;
using Holmes.Customers.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql.Repositories;

public class SqlCustomerRepository(CustomersDbContext dbContext)
    : ICustomerRepository
{
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        var db = CustomerMapper.ToDb(customer);
        dbContext.Customers.Add(db);
        UpsertDirectory(db);
        await Task.CompletedTask;
    }

    public async Task<Customer?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var spec = new CustomerWithAdminsByIdSpec(id.ToString());

        var db = await dbContext.Customers
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return db is null ? null : CustomerMapper.ToDomain(db);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken)
    {
        var spec = new CustomerWithAdminsByIdSpec(customer.Id.ToString());

        var db = await dbContext.Customers
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (db is null)
        {
            throw new InvalidOperationException($"Customer '{customer.Id}' not found.");
        }

        CustomerMapper.UpdateDb(db, customer);
        UpsertDirectory(db);
    }

    private void UpsertDirectory(CustomerDb db)
    {
        var directory = dbContext.CustomerDirectory
            .SingleOrDefault(c => c.CustomerId == db.CustomerId);

        if (directory is null)
        {
            directory = new CustomerDirectoryDb
            {
                CustomerId = db.CustomerId,
                Name = db.Name,
                Status = db.Status,
                CreatedAt = db.CreatedAt,
                AdminCount = db.Admins.Count
            };
            dbContext.CustomerDirectory.Add(directory);
        }
        else
        {
            directory.Name = db.Name;
            directory.Status = db.Status;
            directory.AdminCount = db.Admins.Count;
        }
    }
}
