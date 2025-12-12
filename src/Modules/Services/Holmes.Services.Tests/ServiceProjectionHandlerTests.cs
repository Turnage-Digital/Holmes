using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Projections;
using Holmes.Services.Application.EventHandlers;
using Holmes.Services.Domain;
using Holmes.Services.Domain.Events;
using Moq;

namespace Holmes.Services.Tests;

public sealed class ServiceProjectionHandlerTests
{
    private Mock<IServiceProjectionWriter> _writerMock = null!;
    private ServiceProjectionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _writerMock = new Mock<IServiceProjectionWriter>();
        _handler = new ServiceProjectionHandler(_writerMock.Object);
    }

    [Test]
    public async Task Handle_ServiceRequestCreated_UpsertsProjection()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var createdAt = DateTimeOffset.UtcNow;

        var notification = new ServiceRequestCreated(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            ServiceCategory.Identity,
            Tier: 1,
            ScopeType: null,
            ScopeValue: null,
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
            Assert.That(capturedModel!.ServiceRequestId, Is.EqualTo(serviceRequestId.ToString()));
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
    public async Task Handle_ServiceRequestCreated_WithScope_UpsertsProjection()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var createdAt = DateTimeOffset.UtcNow;

        var notification = new ServiceRequestCreated(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.CountySearch.Code,
            ServiceCategory.Criminal,
            Tier: 2,
            ScopeType: "County",
            ScopeValue: "48201",
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
    public async Task Handle_ServiceRequestDispatched_UpdatesDispatchedInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var dispatchedAt = DateTimeOffset.UtcNow;
        const string vendorCode = "STUB";
        const string vendorReferenceId = "REF-12345";

        var notification = new ServiceRequestDispatched(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            vendorCode,
            vendorReferenceId,
            dispatchedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateDispatchedAsync(
                serviceRequestId.ToString(),
                vendorCode,
                vendorReferenceId,
                dispatchedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRequestDispatched_WithNullVendorRef_UpdatesDispatchedInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var dispatchedAt = DateTimeOffset.UtcNow;
        const string vendorCode = "STUB";

        var notification = new ServiceRequestDispatched(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            vendorCode,
            VendorReferenceId: null,
            dispatchedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateDispatchedAsync(
                serviceRequestId.ToString(),
                vendorCode,
                null,
                dispatchedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRequestInProgress_UpdatesInProgressInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var updatedAt = DateTimeOffset.UtcNow;

        var notification = new ServiceRequestInProgress(
            serviceRequestId,
            orderId,
            ServiceType.SsnTrace.Code,
            "STUB",
            updatedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateInProgressAsync(
                serviceRequestId.ToString(),
                updatedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRequestCompleted_UpdatesCompletedInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var completedAt = DateTimeOffset.UtcNow;
        const int recordCount = 3;

        var notification = new ServiceRequestCompleted(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.CountySearch.Code,
            ServiceResultStatus.Hit,
            recordCount,
            completedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateCompletedAsync(
                serviceRequestId.ToString(),
                ServiceResultStatus.Hit,
                recordCount,
                completedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRequestCompleted_WithClear_UpdatesCompletedInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var completedAt = DateTimeOffset.UtcNow;

        var notification = new ServiceRequestCompleted(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            ServiceResultStatus.Clear,
            RecordCount: 0,
            completedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateCompletedAsync(
                serviceRequestId.ToString(),
                ServiceResultStatus.Clear,
                0,
                completedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRequestFailed_UpdatesFailedInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var failedAt = DateTimeOffset.UtcNow;
        const string errorMessage = "Connection timeout";
        const int attemptCount = 2;

        var notification = new ServiceRequestFailed(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            errorMessage,
            attemptCount,
            MaxAttempts: 3,
            WillRetry: true,
            failedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateFailedAsync(
                serviceRequestId.ToString(),
                errorMessage,
                attemptCount,
                true,
                failedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRequestFailed_NoRetry_UpdatesFailedInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var failedAt = DateTimeOffset.UtcNow;
        const string errorMessage = "Fatal error";
        const int attemptCount = 3;

        var notification = new ServiceRequestFailed(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            errorMessage,
            attemptCount,
            MaxAttempts: 3,
            WillRetry: false,
            failedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateFailedAsync(
                serviceRequestId.ToString(),
                errorMessage,
                attemptCount,
                false,
                failedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRequestCanceled_UpdatesCanceledInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var canceledAt = DateTimeOffset.UtcNow;
        const string reason = "Order canceled by customer";

        var notification = new ServiceRequestCanceled(
            serviceRequestId,
            orderId,
            customerId,
            ServiceType.SsnTrace.Code,
            reason,
            canceledAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateCanceledAsync(
                serviceRequestId.ToString(),
                reason,
                canceledAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ServiceRequestRetried_UpdatesRetriedInfo()
    {
        var serviceRequestId = UlidId.NewUlid();
        var orderId = UlidId.NewUlid();
        var retriedAt = DateTimeOffset.UtcNow;
        const int attemptCount = 2;

        var notification = new ServiceRequestRetried(
            serviceRequestId,
            orderId,
            ServiceType.SsnTrace.Code,
            attemptCount,
            retriedAt);

        await _handler.Handle(notification, CancellationToken.None);

        _writerMock.Verify(
            x => x.UpdateRetriedAsync(
                serviceRequestId.ToString(),
                attemptCount,
                retriedAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
