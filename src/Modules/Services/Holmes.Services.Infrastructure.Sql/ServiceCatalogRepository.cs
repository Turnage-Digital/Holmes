using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql;

public sealed class ServiceCatalogRepository(ServicesDbContext dbContext) : IServiceCatalogRepository
{
    public async Task SaveSnapshotAsync(
        string customerId,
        int version,
        string configJson,
        string createdBy,
        CancellationToken cancellationToken
    )
    {
        var snapshot = new ServiceCatalogSnapshotDb
        {
            Id = Ulid.NewUlid().ToString(),
            CustomerId = customerId,
            Version = version,
            ConfigJson = configJson,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        dbContext.ServiceCatalogSnapshots.Add(snapshot);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}