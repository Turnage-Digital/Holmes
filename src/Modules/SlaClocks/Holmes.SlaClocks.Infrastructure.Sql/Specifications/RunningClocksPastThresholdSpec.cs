using Holmes.Core.Domain.Specifications;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;

namespace Holmes.SlaClocks.Infrastructure.Sql.Specifications;

/// <summary>
///     Finds running clocks that have passed their at-risk threshold but not yet been marked at-risk.
///     Used by the watchdog service to detect clocks approaching their deadline.
/// </summary>
public sealed class RunningClocksPastThresholdSpec : Specification<SlaClockDb>
{
    public RunningClocksPastThresholdSpec(DateTime asOfUtc)
    {
        AddCriteria(c =>
            c.State == (int)ClockState.Running &&
            c.AtRiskAt == null &&
            c.AtRiskThresholdAt <= asOfUtc);
    }
}