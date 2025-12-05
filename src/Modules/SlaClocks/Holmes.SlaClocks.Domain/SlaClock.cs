using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain.Events;

namespace Holmes.SlaClocks.Domain;

public sealed class SlaClock : AggregateRoot
{
    private SlaClock()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId OrderId { get; private set; }
    public UlidId CustomerId { get; private set; }
    public ClockKind Kind { get; private set; }
    public ClockState State { get; private set; }

    // Time tracking
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset DeadlineAt { get; private set; }
    public DateTimeOffset AtRiskThresholdAt { get; private set; }
    public DateTimeOffset? AtRiskAt { get; private set; }
    public DateTimeOffset? BreachedAt { get; private set; }
    public DateTimeOffset? PausedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? PauseReason { get; private set; }

    // Accumulated pause time for deadline adjustment
    public TimeSpan AccumulatedPauseTime { get; private set; }

    // SLA configuration
    public int TargetBusinessDays { get; private set; }
    public decimal AtRiskThresholdPercent { get; private set; }

    public static SlaClock Start(
        UlidId id,
        UlidId orderId,
        UlidId customerId,
        ClockKind kind,
        DateTimeOffset startedAt,
        DateTimeOffset deadlineAt,
        DateTimeOffset atRiskThresholdAt,
        int targetBusinessDays,
        decimal atRiskThresholdPercent = 0.80m)
    {
        var clock = new SlaClock
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            Kind = kind,
            State = ClockState.Running,
            StartedAt = startedAt,
            DeadlineAt = deadlineAt,
            AtRiskThresholdAt = atRiskThresholdAt,
            TargetBusinessDays = targetBusinessDays,
            AtRiskThresholdPercent = atRiskThresholdPercent,
            AccumulatedPauseTime = TimeSpan.Zero
        };

        clock.AddDomainEvent(new SlaClockStarted(
            id,
            orderId,
            customerId,
            kind,
            startedAt,
            deadlineAt,
            atRiskThresholdAt,
            targetBusinessDays));

        return clock;
    }

    public static SlaClock Rehydrate(
        UlidId id,
        UlidId orderId,
        UlidId customerId,
        ClockKind kind,
        ClockState state,
        DateTimeOffset startedAt,
        DateTimeOffset deadlineAt,
        DateTimeOffset atRiskThresholdAt,
        DateTimeOffset? atRiskAt,
        DateTimeOffset? breachedAt,
        DateTimeOffset? pausedAt,
        DateTimeOffset? completedAt,
        string? pauseReason,
        TimeSpan accumulatedPauseTime,
        int targetBusinessDays,
        decimal atRiskThresholdPercent)
    {
        return new SlaClock
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            Kind = kind,
            State = state,
            StartedAt = startedAt,
            DeadlineAt = deadlineAt,
            AtRiskThresholdAt = atRiskThresholdAt,
            AtRiskAt = atRiskAt,
            BreachedAt = breachedAt,
            PausedAt = pausedAt,
            CompletedAt = completedAt,
            PauseReason = pauseReason,
            AccumulatedPauseTime = accumulatedPauseTime,
            TargetBusinessDays = targetBusinessDays,
            AtRiskThresholdPercent = atRiskThresholdPercent
        };
    }

    public void Pause(string reason, DateTimeOffset pausedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (State is ClockState.Completed or ClockState.Breached)
        {
            return; // Already terminal
        }

        if (State == ClockState.Paused)
        {
            PauseReason = reason; // Update reason
            return;
        }

        PausedAt = pausedAt;
        PauseReason = reason;
        State = ClockState.Paused;

        AddDomainEvent(new SlaClockPaused(Id, OrderId, CustomerId, Kind, reason, pausedAt));
    }

    public void Resume(DateTimeOffset resumedAt)
    {
        if (State != ClockState.Paused)
        {
            return;
        }

        if (PausedAt is null)
        {
            throw new InvalidOperationException("Clock is paused but has no pause timestamp.");
        }

        var pauseDuration = resumedAt - PausedAt.Value;
        AccumulatedPauseTime += pauseDuration;

        // Adjust deadline and at-risk threshold by pause duration
        DeadlineAt = DeadlineAt.Add(pauseDuration);
        AtRiskThresholdAt = AtRiskThresholdAt.Add(pauseDuration);

        // Determine what state to return to
        State = AtRiskAt.HasValue ? ClockState.AtRisk : ClockState.Running;
        PausedAt = null;
        PauseReason = null;

        AddDomainEvent(new SlaClockResumed(Id, OrderId, CustomerId, Kind, resumedAt, pauseDuration));
    }

    public void MarkAtRisk(DateTimeOffset atRiskAt)
    {
        if (State is not (ClockState.Running or ClockState.Paused))
        {
            return; // Already at-risk, breached, or completed
        }

        AtRiskAt = atRiskAt;

        if (State == ClockState.Running)
        {
            State = ClockState.AtRisk;
        }
        // If paused, stay paused but record at-risk timestamp

        AddDomainEvent(new SlaClockAtRisk(Id, OrderId, CustomerId, Kind, atRiskAt, DeadlineAt));
    }

    public void MarkBreached(DateTimeOffset breachedAt)
    {
        if (State is ClockState.Completed or ClockState.Breached)
        {
            return; // Already terminal
        }

        BreachedAt = breachedAt;
        State = ClockState.Breached;

        AddDomainEvent(new SlaClockBreached(Id, OrderId, CustomerId, Kind, breachedAt, DeadlineAt));
    }

    public void Complete(DateTimeOffset completedAt)
    {
        if (State is ClockState.Completed or ClockState.Breached)
        {
            return; // Already terminal
        }

        CompletedAt = completedAt;
        var wasAtRisk = AtRiskAt.HasValue;
        var totalElapsed = completedAt - StartedAt - AccumulatedPauseTime;

        State = ClockState.Completed;

        AddDomainEvent(new SlaClockCompleted(
            Id,
            OrderId,
            CustomerId,
            Kind,
            completedAt,
            DeadlineAt,
            wasAtRisk,
            totalElapsed));
    }

    /// <summary>
    /// Returns true if the clock is in a terminal state (completed or breached).
    /// </summary>
    public bool IsTerminal => State is ClockState.Completed or ClockState.Breached;

    /// <summary>
    /// Returns true if the clock is actively tracking time (running or at-risk).
    /// </summary>
    public bool IsActive => State is ClockState.Running or ClockState.AtRisk;
}
