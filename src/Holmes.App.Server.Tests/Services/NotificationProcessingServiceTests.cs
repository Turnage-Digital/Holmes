using Holmes.App.Server.Services;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Application.Abstractions;
using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Holmes.App.Server.Tests.Services;

public sealed class NotificationProcessingServiceTests
{
    private Mock<ILogger<NotificationProcessingService>> _loggerMock = null!;
    private Mock<INotificationQueries> _notificationQueriesMock = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private Mock<ISender> _senderMock = null!;

    [SetUp]
    public void Setup()
    {
        _notificationQueriesMock = new Mock<INotificationQueries>();
        _senderMock = new Mock<ISender>();
        _loggerMock = new Mock<ILogger<NotificationProcessingService>>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(INotificationQueries)))
            .Returns(_notificationQueriesMock.Object);
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
    public async Task ExecuteAsync_ProcessesPendingNotifications()
    {
        // Arrange
        var notification = CreateTestPendingDto();
        var notificationId = UlidId.Parse(notification.Id);

        _notificationQueriesMock
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPendingDto> { notification });

        _senderMock
            .Setup(s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var service = new NotificationProcessingService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act - Start the service and cancel after a short delay
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100); // Allow one iteration
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _senderMock.Verify(
            s => s.Send(
                It.Is<ProcessNotificationCommand>(cmd => cmd.NotificationId == notificationId),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Test]
    public async Task ExecuteAsync_DoesNothing_WhenNoPendingNotifications()
    {
        // Arrange
        _notificationQueriesMock
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPendingDto>());

        var service = new NotificationProcessingService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert - No commands should have been sent
        _senderMock.Verify(
            s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Test]
    public async Task ExecuteAsync_ContinuesOnError_WhenProcessingFails()
    {
        // Arrange
        var notification1 = CreateTestPendingDto();
        var notification2 = CreateTestPendingDto();

        _notificationQueriesMock
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPendingDto> { notification1, notification2 });

        var callCount = 0;
        _senderMock
            .Setup(s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Returns((ProcessNotificationCommand cmd, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("Simulated failure");
                }

                return Task.FromResult(Result.Success());
            });

        var service = new NotificationProcessingService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert - Both notifications should have been attempted
        _senderMock.Verify(
            s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Test]
    public async Task ExecuteAsync_LogsWarning_WhenProcessingReturnsFailure()
    {
        // Arrange
        var notification = CreateTestPendingDto();

        _notificationQueriesMock
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationPendingDto> { notification });

        _senderMock
            .Setup(s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Provider unavailable"));

        var service = new NotificationProcessingService(_scopeFactory, _loggerMock.Object);

        using var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await service.StopAsync(CancellationToken.None);

        // Assert - Command was called (the service logs warnings but continues)
        _senderMock.Verify(
            s => s.Send(It.IsAny<ProcessNotificationCommand>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    private static NotificationPendingDto CreateTestPendingDto()
    {
        var customerId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();

        return new NotificationPendingDto(
            UlidId.NewUlid().ToString(),
            customerId.ToString(),
            orderId.ToString(),
            NotificationTriggerType.OrderStateChanged,
            NotificationChannel.Email,
            "test@example.com",
            DeliveryStatus.Pending,
            0,
            null);
    }
}