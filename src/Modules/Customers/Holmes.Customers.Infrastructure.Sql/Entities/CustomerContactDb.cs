namespace Holmes.Customers.Infrastructure.Sql.Entities;

public class CustomerContactDb
{
    public string ContactId { get; set; } = null!;

    public string CustomerId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Role { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public CustomerProfileDb Customer { get; set; } = null!;
}
