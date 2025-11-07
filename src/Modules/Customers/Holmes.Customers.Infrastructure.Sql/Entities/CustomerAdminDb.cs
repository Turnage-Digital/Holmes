using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Customers.Infrastructure.Sql.Entities;

public class CustomerAdminDb
{
    public long Id { get; set; }

    public string CustomerId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public UlidId AssignedBy { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    public CustomerDb Customer { get; set; } = null!;
}