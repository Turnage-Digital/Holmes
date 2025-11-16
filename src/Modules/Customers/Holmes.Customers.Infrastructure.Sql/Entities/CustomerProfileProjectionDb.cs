namespace Holmes.Customers.Infrastructure.Sql.Entities;

public class CustomerProfileProjectionDb
{
    public string CustomerId { get; set; } = null!;

    public string TenantId { get; set; } = null!;

    public string PolicySnapshotId { get; set; } = null!;

    public string? BillingEmail { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CustomerContactProjectionDb> Contacts { get; set; } = new List<CustomerContactProjectionDb>();
}
