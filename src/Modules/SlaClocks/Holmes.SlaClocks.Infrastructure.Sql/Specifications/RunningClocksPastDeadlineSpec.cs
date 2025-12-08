using Holmes.Core.Domain.Specifications;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;

namespace Holmes.SlaClocks.Infrastructure.Sql.Specifications;

/// <summary>
///     Finds running or at-risk clocks that have passed their deadline but not yet been marked as breached.
///     Used by the watchdog service to detect SLA violations.
/// </summary>
public sealed class RunningClocksPastDeadlineSpec : Specification<SlaClockDb>
{
    private static readonly int[] ActiveStates = [(int)ClockState.Running, (int)ClockState.AtRisk];

    public RunningClocksPastDeadlineSpec(DateTime asOfUtc)
    {
        AddCriteria(c =>
            ActiveStates.Contains(c.State) &&
            c.BreachedAt == null &&
            c.DeadlineAt <= asOfUtc);
    }
}