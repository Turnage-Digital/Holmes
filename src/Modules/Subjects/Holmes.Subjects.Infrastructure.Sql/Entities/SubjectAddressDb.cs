namespace Holmes.Subjects.Infrastructure.Sql.Entities;

public class SubjectAddressDb
{
    public string Id { get; set; } = null!;

    public string SubjectId { get; set; } = null!;

    public string Street1 { get; set; } = null!;

    public string? Street2 { get; set; }

    public string City { get; set; } = null!;

    public string State { get; set; } = null!;

    public string PostalCode { get; set; } = null!;

    public string Country { get; set; } = null!;

    public string? CountyFips { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public int AddressType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public SubjectDb Subject { get; set; } = null!;
}
