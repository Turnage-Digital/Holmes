using Holmes.Core.Domain.Specifications;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class ServiceRequestsByOrderIdSpec : Specification<ServiceRequestDb>
{
    public ServiceRequestsByOrderIdSpec(string orderId)
    {
        AddCriteria(r => r.OrderId == orderId);
        AddInclude(r => r.Result!);
    }
}