using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql;

public sealed class CustomerAccessQueries(CustomersDbContext dbContext) : ICustomerAccessQueries
{
    public async Task<IReadOnlyCollection<string>> GetAdminCustomerIdsAsync(
        UlidId userId,
        CancellationToken cancellationToken
    )
    {
        var list = await dbContext.CustomerAdmins
            .AsNoTracking()
            .Where(a => a.UserId == userId.ToString())
            .Select(a => a.CustomerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return list;
    }
}