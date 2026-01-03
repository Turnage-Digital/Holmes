using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.EventHandlers;
using Holmes.Services.Contracts;
using Holmes.Services.Domain;
using Holmes.Services.Domain.Events;
using Moq;

namespace Holmes.Services.Tests;

public sealed class ServiceProjectionHandlerTests
{
    private ServiceProjectionHandler _handler = null!;
    private Mock<IServiceProjectionWriter> _writerMock = null!;

    [SetUp]
    public void SetUp()
    {
        _writerMock = new Mock<IServiceProjectionWriter>();
        _handler = new ServiceProjectionHandler(_writerMock.Object);
    }

    [Test]
    public async Task Handle_ServiceCreated_UpsertsProjection()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var createdAt = DateTimeOffset.UtcNow;

        var notification = new ServiceCreated(
            serviceId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            ServiceCategory.Identity,
            1,
            null,
            null,
            createdAt);

        ServiceProjectionModel? capturedModel = null;
        _writerMock
            .Setup(x => x.UpsertAsync(It.IsAny<ServiceProjectionModel>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceProjectionModel, CancellationToken>((m, _) => capturedModel = m)
            .Returns(Task.CompletedTask);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpsertAsync(It.IsAny<ServiceProjectionModel>(), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.That(capturedModel, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedModel!.ServiceId, Is.EqualTo(serviceId.ToString()));
            Assert.That(capturedModel.OrderId, Is.EqualTo(orderId.ToString()));
            Assert.That(capturedModel.CustomerId, Is.EqualTo(customerId.ToString()));
            Assert.That(capturedModel.ServiceTypeCode, Is.EqualTo(ServiceType.SsnTrace.Code));
            Assert.That(capturedModel.Category, Is.EqualTo(ServiceCategory.Identity));
            Assert.That(capturedModel.Status, Is.EqualTo(ServiceStatus.Pending));
            Assert.That(capturedModel.Tier, Is.EqualTo(1));
            Assert.That(capturedModel.ScopeType, Is.Null);
            Assert.That(capturedModel.ScopeValue, Is.Null);
            Assert.That(capturedModel.CreatedAt, Is.EqualTo(createdAt));
        });
    }

    [Test]
    public async Task Handle_ServiceCreated_WithScope_UpsertsProjection()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var createdAt = DateTimeOffset.UtcNow;

        var notification = new ServiceCreated(
            serviceId,
            orderId,
            customerId,
            ServiceType.CountySearch.Code,
            ServiceCategory.Criminal,
            2,
            "County",
            "48201",
            createdAt);

        ServiceProjectionModel? capturedModel = null;
        _writerMock
            .Setup(x => x.UpsertAsync(It.IsAny<ServiceProjectionModel>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceProjectionModel, CancellationToken>((m, _) => capturedModel = m)
            .Returns(Task.CompletedTask);

        await _handler.Handle(notification, CancellationToken.None);

        Assert.That(capturedModel, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedModel!.ServiceTypeCode, Is.EqualTo(ServiceType.CountySearch.Code));
            Assert.That(capturedModel.Category, Is.EqualTo(ServiceCategory.Criminal));
            Assert.That(capturedModel.Tier, Is.EqualTo(2));
            Assert.That(capturedModel.ScopeType, Is.EqualTo("County"));
            Assert.That(capturedModel.ScopeValue, Is.EqualTo("48201"));
        });
    }

    [Test]
    public async Task Handle_ServiceDispatched_UpdatesDispatchedInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var dispatchedAt = DateTimeOffset.UtcNow;
        const string vendorCode = "STUB";
        const string vendorReferenceId = "REF-12345";

        var notification = new ServiceDispatched(
            serviceId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            vendorCode,
            vendorReferenceId,
            dispatchedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateDispatchedAsync(
                serviceId.ToString(),
                vendorCode,
                vendorReferenceId,
                dispatchedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceDispatched_WithNullVendorRef_UpdatesDispatchedInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var dispatchedAt = DateTimeOffset.UtcNow;
        const string vendorCode = "STUB";

        var notification = new ServiceDispatched(
            serviceId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            vendorCode,
            null,
            dispatchedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateDispatchedAsync(
                serviceId.ToString(),
                vendorCode,
                null,
                dispatchedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceInProgress_UpdatesInProgressInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var updatedAt = DateTimeOffset.UtcNow;

        var notification = new ServiceInProgress(
            serviceId,
            orderId,
            ServiceType.SsnTrace.Code,
            "STUB",
            updatedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateInProgressAsync(
                serviceId.ToString(),
                updatedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceCompleted_UpdatesCompletedInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var completedAt = DateTimeOffset.UtcNow;
        const int recordCount = 3;

        var notification = new ServiceCompleted(
            serviceId,
            orderId,
            customerId,
            ServiceType.CountySearch.Code,
            ServiceResultStatus.Hit,
            recordCount,
            completedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateCompletedAsync(
                serviceId.ToString(),
                ServiceResultStatus.Hit,
                recordCount,
                completedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceCompleted_WithClear_UpdatesCompletedInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var completedAt = DateTimeOffset.UtcNow;

        var notification = new ServiceCompleted(
            serviceId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            ServiceResultStatus.Clear,
            0,
            completedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateCompletedAsync(
                serviceId.ToString(),
                ServiceResultStatus.Clear,
                0,
                completedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceFailed_UpdatesFailedInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var failedAt = DateTimeOffset.UtcNow;
        const string errorMessage = "Connection timeout";
        const int attemptCount = 2;

        var notification = new ServiceFailed(
            serviceId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            errorMessage,
            attemptCount,
            3,
            true,
            failedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateFailedAsync(
                serviceId.ToString(),
                errorMessage,
                attemptCount,
                true,
                failedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceFailed_NoRetry_UpdatesFailedInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var failedAt = DateTimeOffset.UtcNow;
        const string errorMessage = "Fatal error";
        const int attemptCount = 3;

        var notification = new ServiceFailed(
            serviceId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            errorMessage,
            attemptCount,
            3,
            false,
            failedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateFailedAsync(
                serviceId.ToString(),
                errorMessage,
                attemptCount,
                false,
                failedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceCanceled_UpdatesCanceledInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var canceledAt = DateTimeOffset.UtcNow;
        const string reason = "Order canceled by customer";

        var notification = new ServiceCanceled(
            serviceId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            reason,
            canceledAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateCanceledAsync(
                serviceId.ToString(),
                reason,
                canceledAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRetried_UpdatesRetriedInfo()
    {
        var serviceId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var retriedAt = DateTimeOffset.UtcNow;
        const int attemptCount = 2;

        var notification = new ServiceRetried(
            serviceId,
            orderId,
            ServiceType.SsnTrace.Code,
            attemptCount,
            retriedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateRetriedAsync(
                serviceId.ToString(),
                attemptCount,
                retriedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}