namespace Holmes.Customers.Infrastructure.Sql.Entities;

public class CustomerProfileDb
{
    public string CustomerId { get; set; } = null!;

    public string PolicySnapshotId { get; set; } = null!;

    public string? BillingEmail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CustomerContactDb> Contacts { get; set; } = new List<CustomerContactDb>();
}