using Holmes.App.Server.Services;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Application.Commands;
using Holmes.SlaClocks.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Holmes.App.Server.Tests.Services;

public sealed class SlaClockWatchdogServiceTests
{
    private Mock<ILogger<SlaClockWatchdogService>> _loggerMock = null!;
    private Mock<ISlaClockRepository> _repositoryMock = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private Mock<ISender> _senderMock = null!;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ISlaClockRepository>();
        _senderMock = new Mock<ISender>();
        _loggerMock = new Mock<ILogger<SlaClockWatchdogService>>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ISlaClockRepository)))
            .Returns(_repositoryMock.Object);
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ISender)))
            .Returns(_senderMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(scopeMock.Object);

        _scopeFactory = scopeFactoryMock.Object;
    }

    [Test]
    public async Task ExecuteAsync_MarksAtRiskClocks_WhenPastThreshold()
    {
        // Arrange
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var clock = CreateTestClock(clockId, orderId, ClockState.Running);

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastThresholdAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock> { clock });

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastDeadlineAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock>());

        _senderMock
            .Setup(s => s.Send(It.IsAny<MarkClockAtRiskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new SlaClockWatchdogService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _senderMock.Verify(
            s => s.Send(
                It.Is<MarkClockAtRiskCommand>(cmd => cmd.ClockId == clockId),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Test]
    public async Task ExecuteAsync_MarksBreachedClocks_WhenPastDeadline()
    {
        // Arrange
        var clockId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var clock = CreateTestClock(clockId, orderId, ClockState.AtRisk);

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastThresholdAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock>());

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastDeadlineAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock> { clock });

        _senderMock
            .Setup(s => s.Send(It.IsAny<MarkClockBreachedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new SlaClockWatchdogService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _senderMock.Verify(
            s => s.Send(
                It.Is<MarkClockBreachedCommand>(cmd => cmd.ClockId == clockId),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Test]
    public async Task ExecuteAsync_DoesNothing_WhenNoClocksNeedAttention()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetRunningClocksPastThresholdAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock>());

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastDeadlineAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock>());

        var service = new SlaClockWatchdogService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert - No commands should have been sent
        _senderMock.Verify(
            s => s.Send(It.IsAny<MarkClockAtRiskCommand>(), It.IsAny<CancellationToken>()),
            Times.Never());
        _senderMock.Verify(
            s => s.Send(It.IsAny<MarkClockBreachedCommand>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Test]
    public async Task ExecuteAsync_ContinuesProcessing_WhenOneClockFails()
    {
        // Arrange
        var clock1 = CreateTestClock(UlidId.NewUlid(), UlidId.NewUlid(), ClockState.Running);
        var clock2 = CreateTestClock(UlidId.NewUlid(), UlidId.NewUlid(), ClockState.Running);

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastThresholdAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock> { clock1, clock2 });

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastDeadlineAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock>());

        var callCount = 0;
        _senderMock
            .Setup(s => s.Send(It.IsAny<MarkClockAtRiskCommand>(), It.IsAny<CancellationToken>()))
            .Returns((MarkClockAtRiskCommand cmd, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Simulated failure");
                }

                return Task.FromResult(Result.Success());
            });

        var service = new SlaClockWatchdogService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert - Both clocks should have been attempted
        _senderMock.Verify(
            s => s.Send(It.IsAny<MarkClockAtRiskCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Test]
    public async Task ExecuteAsync_HandlesMultipleClocksInBothCategories()
    {
        // Arrange
        var atRiskClock = CreateTestClock(UlidId.NewUlid(), UlidId.NewUlid(), ClockState.Running);
        var breachedClock = CreateTestClock(UlidId.NewUlid(), UlidId.NewUlid(), ClockState.AtRisk);

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastThresholdAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock> { atRiskClock });

        _repositoryMock
            .Setup(r => r.GetRunningClocksPastDeadlineAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SlaClock> { breachedClock });

        _senderMock
            .Setup(s => s.Send(It.IsAny<MarkClockAtRiskCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _senderMock
            .Setup(s => s.Send(It.IsAny<MarkClockBreachedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new SlaClockWatchdogService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert - Both types of commands should have been sent
        _senderMock.Verify(
            s => s.Send(It.IsAny<MarkClockAtRiskCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
        _senderMock.Verify(
            s => s.Send(It.IsAny<MarkClockBreachedCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    private static SlaClock CreateTestClock(UlidId clockId, UlidId orderId, ClockState state)
    {
        var startedAt = DateTimeOffset.UtcNow.AddDays(-2);
        var deadline = startedAt.AddDays(3);
        var atRiskThreshold = startedAt.AddDays(2);

        return SlaClock.Rehydrate(
            clockId,
            orderId,
            UlidId.NewUlid(),
            ClockKind.Fulfillment,
            state,
            startedAt,
            deadline,
            atRiskThreshold,
            state == ClockState.AtRisk ? DateTimeOffset.UtcNow.AddHours(-1) : null,
            null,
            null,
            null,
            null,
            TimeSpan.Zero,
            3,
            0.80m);
    }
}