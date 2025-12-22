using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using Holmes.Services.Domain.Events;

namespace Holmes.Services.Tests;

public sealed class ServiceTests
{
    private static Service CreatePendingRequest()
    {
        return Service.Create(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            ServiceType.SsnTrace,
            1,
            null,
            null,
            DateTimeOffset.UtcNow);
    }

    [Test]
    public void Create_InitializesWithPendingStatus()
    {
        var orderId = UlidId.NewUlid();
        var customerId = UlidId.NewUlid();
        var now = DateTimeOffset.UtcNow;

        var request = Service.Create(
            UlidId.NewUlid(),
            orderId,
            customerId,
            ServiceType.FederalCriminal,
            2,
            ServiceScope.National(),
            null,
            now);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(ServiceStatus.Pending));
            Assert.That(request.OrderId, Is.EqualTo(orderId));
            Assert.That(request.CustomerId, Is.EqualTo(customerId));
            Assert.That(request.ServiceTypeCode, Is.EqualTo(ServiceType.FederalCriminal.Code));
            Assert.That(request.Category, Is.EqualTo(ServiceCategory.Criminal));
            Assert.That(request.Tier, Is.EqualTo(2));
            Assert.That(request.AttemptCount, Is.EqualTo(0));
            Assert.That(request.IsTerminal, Is.False);
        });
    }

    [Test]
    public void Create_RaisesServiceRequestCreatedEvent()
    {
        var request = CreatePendingRequest();

        var events = request.DomainEvents.OfType<ServiceRequestCreated>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].ServiceRequestId, Is.EqualTo(request.Id));
            Assert.That(events[0].ServiceTypeCode, Is.EqualTo(ServiceType.SsnTrace.Code));
        });
    }

    [Test]
    public void AssignVendor_SetsVendorCode()
    {
        var request = CreatePendingRequest();
        var now = DateTimeOffset.UtcNow;

        request.AssignVendor("STUB", now);

        Assert.That(request.VendorCode, Is.EqualTo("STUB"));
    }

    [Test]
    public void AssignVendor_WhenNotPending_Throws()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            request.AssignVendor("OTHER", DateTimeOffset.UtcNow));
    }

    [Test]
    public void Dispatch_TransitionsToDispatchedAndIncrementsAttemptCount()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;

        request.Dispatch("REF-123", now);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(ServiceStatus.Dispatched));
            Assert.That(request.VendorReferenceId, Is.EqualTo("REF-123"));
            Assert.That(request.DispatchedAt, Is.EqualTo(now));
            Assert.That(request.AttemptCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Dispatch_RaisesServiceRequestDispatchedEvent()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.ClearDomainEvents();

        request.Dispatch("REF-123", DateTimeOffset.UtcNow);

        var events = request.DomainEvents.OfType<ServiceRequestDispatched>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].VendorCode, Is.EqualTo("STUB"));
            Assert.That(events[0].VendorReferenceId, Is.EqualTo("REF-123"));
        });
    }

    [Test]
    public void Dispatch_WithoutVendorAssignment_Throws()
    {
        var request = CreatePendingRequest();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.Dispatch("REF-123", DateTimeOffset.UtcNow));

        Assert.That(ex!.Message, Does.Contain("vendor"));
    }

    [Test]
    public void MarkInProgress_TransitionsFromDispatchedToInProgress()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);

        request.MarkInProgress(DateTimeOffset.UtcNow);

        Assert.That(request.Status, Is.EqualTo(ServiceStatus.InProgress));
    }

    [Test]
    public void MarkInProgress_WhenNotDispatched_IsIdempotent()
    {
        var request = CreatePendingRequest();

        // Should not throw, just be a no-op
        request.MarkInProgress(DateTimeOffset.UtcNow);

        Assert.That(request.Status, Is.EqualTo(ServiceStatus.Pending));
    }

    [Test]
    public void RecordResult_TransitionsToCompletedAndStoresResult()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.MarkInProgress(DateTimeOffset.UtcNow);

        var result = ServiceResult.Clear(UlidId.NewUlid(), "REF-123", DateTimeOffset.UtcNow);
        var completedAt = DateTimeOffset.UtcNow;

        request.RecordResult(result, completedAt);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(ServiceStatus.Completed));
            Assert.That(request.Result, Is.EqualTo(result));
            Assert.That(request.CompletedAt, Is.EqualTo(completedAt));
            Assert.That(request.IsTerminal, Is.True);
        });
    }

    [Test]
    public void RecordResult_RaisesServiceRequestCompletedEvent()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.ClearDomainEvents();

        var result = ServiceResult.Clear(UlidId.NewUlid(), "REF-123", DateTimeOffset.UtcNow);
        request.RecordResult(result, DateTimeOffset.UtcNow);

        var events = request.DomainEvents.OfType<ServiceRequestCompleted>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].ResultStatus, Is.EqualTo(ServiceResultStatus.Clear));
            Assert.That(events[0].RecordCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void Fail_TransitionsToFailedAndRecordsError()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        var failedAt = DateTimeOffset.UtcNow;

        request.Fail("Connection timeout", failedAt);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(ServiceStatus.Failed));
            Assert.That(request.LastError, Is.EqualTo("Connection timeout"));
            Assert.That(request.FailedAt, Is.EqualTo(failedAt));
            Assert.That(request.IsTerminal, Is.True);
        });
    }

    [Test]
    public void Fail_RaisesServiceRequestFailedEvent()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.ClearDomainEvents();

        request.Fail("Connection timeout", DateTimeOffset.UtcNow);

        var events = request.DomainEvents.OfType<ServiceRequestFailed>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].ErrorMessage, Is.EqualTo("Connection timeout"));
            Assert.That(events[0].AttemptCount, Is.EqualTo(1));
            Assert.That(events[0].WillRetry, Is.True);
        });
    }

    [Test]
    public void CanRetry_WhenFailedAndUnderMaxAttempts_IsTrue()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.Fail("Error", DateTimeOffset.UtcNow);

        Assert.That(request.CanRetry, Is.True);
    }

    [Test]
    public void CanRetry_WhenFailedAtMaxAttempts_IsFalse()
    {
        var request = Service.Create(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            ServiceType.SsnTrace,
            1,
            null,
            null,
            DateTimeOffset.UtcNow,
            1);

        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.Fail("Error", DateTimeOffset.UtcNow);

        Assert.That(request.CanRetry, Is.False);
    }

    [Test]
    public void Retry_TransitionsBackToPendingAndClearsError()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.Fail("Error", DateTimeOffset.UtcNow);

        request.Retry(DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(ServiceStatus.Pending));
            Assert.That(request.LastError, Is.Null);
            Assert.That(request.FailedAt, Is.Null);
        });
    }

    [Test]
    public void Retry_RaisesServiceRequestRetriedEvent()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.Fail("Error", DateTimeOffset.UtcNow);
        request.ClearDomainEvents();

        request.Retry(DateTimeOffset.UtcNow);

        var events = request.DomainEvents.OfType<ServiceRequestRetried>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].AttemptCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Retry_WhenCannotRetry_Throws()
    {
        var request = Service.Create(
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            UlidId.NewUlid(),
            ServiceType.SsnTrace,
            1,
            null,
            null,
            DateTimeOffset.UtcNow,
            1);

        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.Fail("Error", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            request.Retry(DateTimeOffset.UtcNow));
    }

    [Test]
    public void Cancel_TransitionsToCanceled()
    {
        var request = CreatePendingRequest();
        var canceledAt = DateTimeOffset.UtcNow;

        request.Cancel("Order canceled", canceledAt);

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(ServiceStatus.Canceled));
            Assert.That(request.CanceledAt, Is.EqualTo(canceledAt));
            Assert.That(request.IsTerminal, Is.True);
        });
    }

    [Test]
    public void Cancel_RaisesServiceRequestCanceledEvent()
    {
        var request = CreatePendingRequest();
        request.ClearDomainEvents();

        request.Cancel("Order canceled", DateTimeOffset.UtcNow);

        var events = request.DomainEvents.OfType<ServiceRequestCanceled>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Reason, Is.EqualTo("Order canceled"));
        });
    }

    [Test]
    public void Cancel_WhenAlreadyCanceled_IsIdempotent()
    {
        var request = CreatePendingRequest();
        request.Cancel("First cancel", DateTimeOffset.UtcNow);
        request.ClearDomainEvents();

        // Should not throw
        request.Cancel("Second cancel", DateTimeOffset.UtcNow);

        Assert.That(request.DomainEvents, Is.Empty);
    }

    [Test]
    public void Cancel_WhenCompleted_Throws()
    {
        var request = CreatePendingRequest();
        request.AssignVendor("STUB", DateTimeOffset.UtcNow);
        request.Dispatch("REF-123", DateTimeOffset.UtcNow);
        request.RecordResult(ServiceResult.Clear(UlidId.NewUlid(), null, DateTimeOffset.UtcNow), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            request.Cancel("Too late", DateTimeOffset.UtcNow));
    }

    [Test]
    public void ServiceType_StaticInstances_HaveCorrectProperties()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ServiceType.SsnTrace.Category, Is.EqualTo(ServiceCategory.Identity));
            Assert.That(ServiceType.SsnTrace.DefaultTier, Is.EqualTo(1));

            Assert.That(ServiceType.FederalCriminal.Category, Is.EqualTo(ServiceCategory.Criminal));
            Assert.That(ServiceType.FederalCriminal.DefaultTier, Is.EqualTo(2));

            Assert.That(ServiceType.DrugTest.Category, Is.EqualTo(ServiceCategory.Drug));
            Assert.That(ServiceType.DrugTest.DefaultTier, Is.EqualTo(4));
        });
    }

    [Test]
    public void ServiceType_FromCode_ReturnsMatchingType()
    {
        var type = ServiceType.FromCode("FED_CRIM");

        Assert.That(type, Is.EqualTo(ServiceType.FederalCriminal));
    }

    [Test]
    public void ServiceType_FromCode_WithUnknownCode_ReturnsNull()
    {
        var type = ServiceType.FromCode("UNKNOWN");

        Assert.That(type, Is.Null);
    }

    [Test]
    public void ServiceScope_State_NormalizesToUppercase()
    {
        var scope = ServiceScope.State("tx");

        Assert.Multiple(() =>
        {
            Assert.That(scope.Type, Is.EqualTo(ServiceScopeType.State));
            Assert.That(scope.Value, Is.EqualTo("TX"));
        });
    }

    [Test]
    public void ServiceResult_Hit_StoresRecords()
    {
        var records = new List<NormalizedRecord>
        {
            new CriminalRecord
            {
                Id = UlidId.NewUlid(),
                CaseNumber = "123",
                Severity = ChargeSeverity.Felony
            }
        };

        var result = ServiceResult.Hit(
            UlidId.NewUlid(),
            records,
            "abc123",
            "REF-123",
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ServiceResultStatus.Hit));
            Assert.That(result.Records, Has.Count.EqualTo(1));
            Assert.That(result.RawResponseHash, Is.EqualTo("abc123"));
        });
    }
}