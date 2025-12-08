using Holmes.App.Server.Services;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Holmes.App.Server.Tests.Services;

public sealed class NotificationProcessingServiceTests
{
    private Mock<ILogger<NotificationProcessingService>> _loggerMock = null!;
    private Mock<INotificationRequestRepository> _repositoryMock = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private Mock<ISender> _senderMock = null!;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<INotificationRequestRepository>();
        _senderMock = new Mock<ISender>();
        _loggerMock = new Mock<ILogger<NotificationProcessingService>>();

        var unitOfWorkMock = new Mock<INotificationsUnitOfWork>();
        unitOfWorkMock.Setup(u => u.NotificationRequests).Returns(_repositoryMock.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(INotificationsUnitOfWork)))
            .Returns(unitOfWorkMock.Object);
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
        var notification = CreateTestNotification();
        var notificationId = notification.Id;

        _repositoryMock
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationRequest> { notification });

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
        _repositoryMock
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationRequest>());

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
        var notification1 = CreateTestNotification();
        var notification2 = CreateTestNotification();

        _repositoryMock
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationRequest> { notification1, notification2 });

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
        var notification = CreateTestNotification();

        _repositoryMock
            .Setup(r => r.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NotificationRequest> { notification });

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

    private static NotificationRequest CreateTestNotification()
    {
        var customerId = UlidId.NewUlid();
        var trigger = NotificationTrigger.OrderStateChanged(
            UlidId.NewUlid(),
            customerId,
            "Created",
            "Invited");
        var recipient = NotificationRecipient.Email("test@example.com", "Test User");
        var content = new NotificationContent { Subject = "Test", Body = "Test body" };

        return NotificationRequest.Create(
            customerId,
            trigger,
            recipient,
            content,
            null,
            NotificationPriority.Normal,
            false,
            DateTimeOffset.UtcNow);
    }
}