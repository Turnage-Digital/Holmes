using Holmes.Core.Domain.Specifications;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;

namespace Holmes.SlaClocks.Infrastructure.Sql.Specifications;

public sealed class SlaClockByOrderIdSpec : Specification<SlaClockDb>
{
    public SlaClockByOrderIdSpec(string orderId)
    {
        AddCriteria(c => c.OrderId == orderId);
    }
}