using Holmes.Customers.Domain;

namespace Holmes.Customers.Infrastructure.Sql.Entities;

public class CustomerDirectoryDb
{
    public string CustomerId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public CustomerStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int AdminCount { get; set; }
}
