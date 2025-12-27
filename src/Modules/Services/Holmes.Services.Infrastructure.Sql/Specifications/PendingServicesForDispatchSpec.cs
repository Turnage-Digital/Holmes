using Holmes.Core.Domain.Specifications;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class PendingServicesForDispatchSpec : Specification<ServiceDb>
{
    public PendingServicesForDispatchSpec(int batchSize)
    {
        AddCriteria(r => r.Status == ServiceStatus.Pending && r.VendorCode != null);
        ApplyOrderBy(r => r.CreatedAt);
        ApplyPaging(0, batchSize);
    }
}