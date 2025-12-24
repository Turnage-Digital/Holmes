using Holmes.Core.Domain.Specifications;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class PendingServicesByTierSpec : Specification<ServiceDb>
{
    public PendingServicesByTierSpec(string orderId, int tier)
    {
        AddCriteria(r => r.OrderId == orderId && r.Tier == tier && r.Status == ServiceStatus.Pending);
        AddInclude(r => r.Result!);
    }
}