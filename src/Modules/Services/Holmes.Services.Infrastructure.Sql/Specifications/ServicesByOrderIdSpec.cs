using Holmes.Core.Domain.Specifications;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class ServicesByOrderIdSpec : Specification<ServiceDb>
{
    public ServicesByOrderIdSpec(string orderId)
    {
        AddCriteria(r => r.OrderId == orderId);
        AddInclude(r => r.Result!);
    }
}