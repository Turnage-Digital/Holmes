using Holmes.Services.Domain;

namespace Holmes.Services.Infrastructure.Sql.Entities;

public class ServiceResultDb
{
    public string Id { get; set; } = null!;
    public string ServiceRequestId { get; set; } = null!;
    public ServiceResultStatus Status { get; set; }
    public string? RecordsJson { get; set; }
    public string? RawResponseHash { get; set; }
    public string? VendorReferenceId { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? NormalizedAt { get; set; }

    public ServiceRequestDb ServiceRequest { get; set; } = null!;
}
