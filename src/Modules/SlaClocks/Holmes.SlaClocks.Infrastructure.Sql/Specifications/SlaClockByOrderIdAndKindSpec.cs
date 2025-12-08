using Holmes.Core.Domain.Specifications;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;

namespace Holmes.SlaClocks.Infrastructure.Sql.Specifications;

public sealed class SlaClockByOrderIdAndKindSpec : Specification<SlaClockDb>
{
    public SlaClockByOrderIdAndKindSpec(string orderId, ClockKind kind)
    {
        AddCriteria(c => c.OrderId == orderId && c.Kind == (int)kind);
        ApplyOrderByDescending(c => c.StartedAt);
    }
}
