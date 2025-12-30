using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Contracts;
using Holmes.SlaClocks.Contracts.Dtos;
using Holmes.SlaClocks.Application.EventHandlers;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Domain.Events;
using Moq;

namespace Holmes.SlaClocks.Tests;

public sealed class SlaClockProjectionHandlerTests
{
    private SlaClockProjectionHandler _handler = null!;
    private Mock<ISlaClockQueries> _queriesMock = null!;
    private Mock<ISlaClockProjectionWriter> _writerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _writerMock = new Mock<ISlaClockProjectionWriter>();
        _queriesMock = new Mock<ISlaClockQueries>();
        _handler = new SlaClockProjectionHandler(_writerMock.Object, _queriesMock.Object);
    }

    [Test]
    public async Task Handle_SlaClockStarted_UpsertsProjection()
    {
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var startedAt = DateTimeOffset.UtcNow;
        var deadlineAt = startedAt.AddDays(3);
        var atRiskThresholdAt = startedAt.AddDays(2);

        var notification = new SlaClockStarted(
            clockId,
            orderId,
            customerId,
            ClockKind.Fulfillment,
            startedAt,
            deadlineAt,
            atRiskThresholdAt,
            3);

        SlaClockProjectionModel? capturedModel = null;
        _writerMock
            .Setup(x => x.UpsertAsync(It.IsAny<SlaClockProjectionModel>(), It.IsAny<CancellationToken>()))
            .Callback<SlaClockProjectionModel, CancellationToken>((m, _) => capturedModel = m)
            .Returns(Task.CompletedTask);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpsertAsync(It.IsAny<SlaClockProjectionModel>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.That(capturedModel, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedModel!.ClockId, Is.EqualTo(clockId.ToString()));
            Assert.That(capturedModel.OrderId, Is.EqualTo(orderId.ToString()));
            Assert.That(capturedModel.CustomerId, Is.EqualTo(customerId.ToString()));
            Assert.That(capturedModel.Kind, Is.EqualTo(ClockKind.Fulfillment));
            Assert.That(capturedModel.State, Is.EqualTo(ClockState.Running));
            Assert.That(capturedModel.StartedAt, Is.EqualTo(startedAt));
            Assert.That(capturedModel.DeadlineAt, Is.EqualTo(deadlineAt));
            Assert.That(capturedModel.AtRiskThresholdAt, Is.EqualTo(atRiskThresholdAt));
            Assert.That(capturedModel.TargetBusinessDays, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task Handle_SlaClockPaused_UpdatesPauseInfo()
    {
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var pausedAt = DateTimeOffset.UtcNow;
        const string reason = "Customer dispute";

        var notification = new SlaClockPaused(
            clockId,
            orderId,
            customerId,
            ClockKind.Fulfillment,
            reason,
            pausedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdatePauseInfoAsync(
                clockId.ToString(),
                ClockState.Paused,
                pausedAt,
                reason,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_SlaClockAtRisk_UpdatesAtRiskInfo()
    {
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var atRiskAt = DateTimeOffset.UtcNow;
        var deadlineAt = atRiskAt.AddDays(1);

        var notification = new SlaClockAtRisk(
            clockId,
            orderId,
            customerId,
            ClockKind.Fulfillment,
            atRiskAt,
            deadlineAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateAtRiskAsync(
                clockId.ToString(),
                ClockState.AtRisk,
                atRiskAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_SlaClockBreached_UpdatesBreachInfo()
    {
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var breachedAt = DateTimeOffset.UtcNow;
        var deadlineAt = breachedAt.AddHours(-1);

        var notification = new SlaClockBreached(
            clockId,
            orderId,
            customerId,
            ClockKind.Fulfillment,
            breachedAt,
            deadlineAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateBreachedAsync(
                clockId.ToString(),
                ClockState.Breached,
                breachedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_SlaClockCompleted_UpdatesCompletionInfo()
    {
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var completedAt = DateTimeOffset.UtcNow;
        var deadlineAt = completedAt.AddDays(1);

        var notification = new SlaClockCompleted(
            clockId,
            orderId,
            customerId,
            ClockKind.Fulfillment,
            completedAt,
            deadlineAt,
            false,
            TimeSpan.FromHours(12));

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateCompletedAsync(
                clockId.ToString(),
                ClockState.Completed,
                completedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_SlaClockResumed_WhenClockFound_UpdatesResumeInfo()
    {
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var resumedAt = DateTimeOffset.UtcNow;
        var pauseDuration = TimeSpan.FromHours(2);

        var notification = new SlaClockResumed(
            clockId,
            orderId,
            customerId,
            ClockKind.Fulfillment,
            resumedAt,
            pauseDuration);

        var clockDto = new SlaClockDto(
            clockId,
            orderId,
            customerId,
            ClockKind.Fulfillment,
            ClockState.Running,
            resumedAt.AddDays(-1),
            resumedAt.AddDays(2),
            resumedAt.AddDays(1),
            null,
            null,
            null,
            null,
            null,
            pauseDuration,
            3,
            0.80m);

        _queriesMock
            .Setup(x => x.GetByOrderIdAndKindAsync(
                orderId.ToString(),
                ClockKind.Fulfillment,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(clockDto);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateResumeInfoAsync(
                clockId.ToString(),
                ClockState.Running,
                clockDto.DeadlineAt,
                clockDto.AtRiskThresholdAt,
                clockDto.AccumulatedPauseTime,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_SlaClockResumed_WhenClockNotFound_FallsBackToStateUpdate()
    {
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var resumedAt = DateTimeOffset.UtcNow;
        var pauseDuration = TimeSpan.FromHours(2);

        var notification = new SlaClockResumed(
            clockId,
            orderId,
            customerId,
            ClockKind.Fulfillment,
            resumedAt,
            pauseDuration);

        _queriesMock
            .Setup(x => x.GetByOrderIdAndKindAsync(
                orderId.ToString(),
                ClockKind.Fulfillment,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((SlaClockDto?)null);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateStateAsync(
                clockId.ToString(),
                ClockState.Running,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _writerMock.Verify(
            x => x.UpdateResumeInfoAsync(
                It.IsAny<string>(),
                It.IsAny<ClockState>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}