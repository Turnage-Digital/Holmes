using Holmes.Core.Domain.Specifications;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;

namespace Holmes.SlaClocks.Infrastructure.Sql.Specifications;

public sealed class ActiveSlaClocksByOrderIdSpec : Specification<SlaClockDb>
{
    private static readonly int[] ActiveStates = [(int)ClockState.Running, (int)ClockState.AtRisk];

    public ActiveSlaClocksByOrderIdSpec(string orderId)
    {
        AddCriteria(c => c.OrderId == orderId && ActiveStates.Contains(c.State));
    }
}
