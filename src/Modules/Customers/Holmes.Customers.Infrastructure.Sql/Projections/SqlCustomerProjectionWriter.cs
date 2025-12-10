using Holmes.Customers.Application.Abstractions.Projections;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql.Projections;

public sealed class SqlCustomerProjectionWriter(CustomersDbContext dbContext) : ICustomerProjectionWriter
{
    public async Task UpsertAsync(CustomerProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.CustomerProjections
            .FirstOrDefaultAsync(x => x.CustomerId == model.CustomerId, cancellationToken);

        if (record is null)
        {
            record = new CustomerProjectionDb
            {
                CustomerId = model.CustomerId
            };
            dbContext.CustomerProjections.Add(record);
        }

        record.Name = model.Name;
        record.Status = model.Status;
        record.CreatedAt = model.CreatedAt;
        record.AdminCount = model.AdminCount;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(string customerId, CustomerStatus status, CancellationToken cancellationToken)
    {
        var record = await dbContext.CustomerProjections
            .FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);

        if (record is not null)
        {
            record.Status = status;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateAdminCountAsync(string customerId, int delta, CancellationToken cancellationToken)
    {
        var record = await dbContext.CustomerProjections
            .FirstOrDefaultAsync(x => x.CustomerId == customerId, cancellationToken);

        if (record is not null)
        {
            record.AdminCount = Math.Max(0, record.AdminCount + delta);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}