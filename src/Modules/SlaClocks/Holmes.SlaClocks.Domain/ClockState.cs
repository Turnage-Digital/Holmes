namespace Holmes.SlaClocks.Domain;

/// <summary>
/// The current state of an SLA clock.
/// </summary>
public enum ClockState
{
    /// <summary>
    /// Clock is actively counting toward deadline.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Clock has passed the at-risk threshold (e.g., 80% of time elapsed)
    /// but has not yet breached. Still counting.
    /// </summary>
    AtRisk = 2,

    /// <summary>
    /// Clock has passed the deadline. SLA is breached.
    /// </summary>
    Breached = 3,

    /// <summary>
    /// Clock is temporarily stopped (e.g., order is blocked).
    /// Time does not count while paused.
    /// </summary>
    Paused = 4,

    /// <summary>
    /// Target was reached before deadline. Clock is stopped successfully.
    /// </summary>
    Completed = 5
}
