namespace Holmes.Services.Infrastructure.Sql.Entities;

public class ServiceCatalogSnapshotDb
{
    public string Id { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public int Version { get; set; }
    public string ConfigJson { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}