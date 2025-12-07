using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;

namespace Holmes.SlaClocks.Tests;

public sealed class SlaClockTests
{
    [Test]
    public void Start_CreatesRunningClock()
    {
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var startedAt = DateTimeOffset.UtcNow;
        var deadline = startedAt.AddDays(3);
        var atRiskThreshold = startedAt.AddDays(2).AddHours(9); // ~80%

        var clock = SlaClock.Start(
            UlidId.NewUlid(),
            orderId,
            customerId,
            ClockKind.Fulfillment,
            startedAt,
            deadline,
            atRiskThreshold,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(clock.State, Is.EqualTo(ClockState.Running));
            Assert.That(clock.Kind, Is.EqualTo(ClockKind.Fulfillment));
            Assert.That(clock.OrderId, Is.EqualTo(orderId));
            Assert.That(clock.CustomerId, Is.EqualTo(customerId));
            Assert.That(clock.StartedAt, Is.EqualTo(startedAt));
            Assert.That(clock.DeadlineAt, Is.EqualTo(deadline));
            Assert.That(clock.AtRiskThresholdAt, Is.EqualTo(atRiskThreshold));
            Assert.That(clock.TargetBusinessDays, Is.EqualTo(3));
            Assert.That(clock.AtRiskThresholdPercent, Is.EqualTo(0.80m));
        });
    }

    [Test]
    public void MarkAtRisk_TransitionsFromRunning()
    {
        var clock = CreateTestClock();
        var atRiskAt = DateTimeOffset.UtcNow.AddDays(2);

        clock.MarkAtRisk(atRiskAt);

        Assert.Multiple(() =>
        {
            Assert.That(clock.State, Is.EqualTo(ClockState.AtRisk));
            Assert.That(clock.AtRiskAt, Is.EqualTo(atRiskAt));
        });
    }

    [Test]
    public void MarkBreached_TransitionsFromAtRisk()
    {
        var clock = CreateTestClock();
        var atRiskAt = DateTimeOffset.UtcNow.AddDays(2);
        var breachedAt = DateTimeOffset.UtcNow.AddDays(3);

        clock.MarkAtRisk(atRiskAt);
        clock.MarkBreached(breachedAt);

        Assert.Multiple(() =>
        {
            Assert.That(clock.State, Is.EqualTo(ClockState.Breached));
            Assert.That(clock.BreachedAt, Is.EqualTo(breachedAt));
        });
    }

    [Test]
    public void Complete_TransitionsFromRunning()
    {
        var clock = CreateTestClock();
        var completedAt = DateTimeOffset.UtcNow.AddDays(1);

        clock.Complete(completedAt);

        Assert.Multiple(() =>
        {
            Assert.That(clock.State, Is.EqualTo(ClockState.Completed));
            Assert.That(clock.CompletedAt, Is.EqualTo(completedAt));
        });
    }

    [Test]
    public void Pause_TransitionsFromRunning()
    {
        var clock = CreateTestClock();
        var pausedAt = DateTimeOffset.UtcNow.AddHours(4);

        clock.Pause("Customer dispute", pausedAt);

        Assert.Multiple(() =>
        {
            Assert.That(clock.State, Is.EqualTo(ClockState.Paused));
            Assert.That(clock.PausedAt, Is.EqualTo(pausedAt));
            Assert.That(clock.PauseReason, Is.EqualTo("Customer dispute"));
        });
    }

    [Test]
    public void Resume_TransitionsFromPaused()
    {
        var clock = CreateTestClock();
        var pausedAt = DateTimeOffset.UtcNow.AddHours(4);
        var resumedAt = pausedAt.AddHours(2);

        clock.Pause("Customer dispute", pausedAt);
        clock.Resume(resumedAt);

        Assert.Multiple(() =>
        {
            Assert.That(clock.State, Is.EqualTo(ClockState.Running));
            Assert.That(clock.PausedAt, Is.Null);
            Assert.That(clock.PauseReason, Is.Null);
            Assert.That(clock.AccumulatedPauseTime, Is.EqualTo(TimeSpan.FromHours(2)));
        });
    }

    [Test]
    public void Complete_IsIdempotent_WhenBreached()
    {
        var clock = CreateTestClock();
        var breachedAt = DateTimeOffset.UtcNow.AddDays(3);
        clock.MarkAtRisk(DateTimeOffset.UtcNow.AddDays(2));
        clock.MarkBreached(breachedAt);

        // Should not throw, just remain breached
        clock.Complete(DateTimeOffset.UtcNow.AddDays(4));

        Assert.Multiple(() =>
        {
            Assert.That(clock.State, Is.EqualTo(ClockState.Breached));
            Assert.That(clock.CompletedAt, Is.Null);
        });
    }

    [Test]
    public void Pause_IsIdempotent_WhenCompleted()
    {
        var clock = CreateTestClock();
        var completedAt = DateTimeOffset.UtcNow.AddDays(1);
        clock.Complete(completedAt);

        // Should not throw, just remain completed
        clock.Pause("Late pause", DateTimeOffset.UtcNow.AddDays(2));

        Assert.Multiple(() =>
        {
            Assert.That(clock.State, Is.EqualTo(ClockState.Completed));
            Assert.That(clock.PausedAt, Is.Null);
        });
    }

    private static SlaClock CreateTestClock()
    {
        var startedAt = DateTimeOffset.UtcNow;
        var deadline = startedAt.AddDays(3);
        var atRiskThreshold = startedAt.AddDays(2).AddHours(9);

        return SlaClock.Start(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            ClockKind.Fulfillment,
            startedAt,
            deadline,
            atRiskThreshold,
            3);
    }
}