using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Mappers;
using Holmes.Customers.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql;

/// <summary>
///     Write-focused repository for Customer aggregate.
///     Query methods are in SqlCustomerQueries (CQRS pattern).
///     Projections are updated via event handlers (CustomerProjectionHandler).
/// </summary>
public class CustomerRepository(CustomersDbContext dbContext)
    : ICustomerRepository
{
    public Task AddAsync(Customer customer, CancellationToken cancellationToken)
    {
        var db = CustomerMapper.ToDb(customer);
        dbContext.Customers.Add(db);
        return Task.CompletedTask;
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
    }
}