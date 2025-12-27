using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain.Events;

namespace Holmes.Services.Domain;

/// <summary>
///     Aggregate root representing a single background check service.
/// </summary>
public sealed class Service : AggregateRoot
{
    private const int DefaultMaxAttempts = 3;

    private Service()
    {
    }

    public UlidId Id { get; private set; }
    public UlidId OrderId { get; private set; }
    public UlidId CustomerId { get; private set; }
    public UlidId? CatalogSnapshotId { get; private set; }

    // Service identification
    public string ServiceTypeCode { get; private set; } = null!;
    public ServiceCategory Category { get; private set; }
    public int Tier { get; private set; }

    // Geographic scope
    public ServiceScopeType? ScopeType { get; private set; }
    public string? ScopeValue { get; private set; }

    // State
    public ServiceStatus Status { get; private set; }

    // Vendor assignment
    public string? VendorCode { get; private set; }
    public string? VendorReferenceId { get; private set; }

    // Retry tracking
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public string? LastError { get; private set; }

    // Timestamps
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public DateTimeOffset? CanceledAt { get; private set; }

    // Result
    public ServiceResult? Result { get; private set; }

    /// <summary>
    ///     Returns true if the request is in a terminal state.
    /// </summary>
    public bool IsTerminal => Status is ServiceStatus.Completed or ServiceStatus.Failed or ServiceStatus.Canceled;

    /// <summary>
    ///     Returns true if the request can be retried.
    /// </summary>
    public bool CanRetry => Status == ServiceStatus.Failed && AttemptCount < MaxAttempts;

    public static Service Create(
        UlidId id,
        UlidId orderId,
        UlidId customerId,
        ServiceType serviceType,
        int tier,
        ServiceScope? scope,
        UlidId? catalogSnapshotId,
        DateTimeOffset createdAt,
        int maxAttempts = DefaultMaxAttempts
    )
    {
        var request = new Service
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            CatalogSnapshotId = catalogSnapshotId,
            ServiceTypeCode = serviceType.Code,
            Category = serviceType.Category,
            Tier = tier,
            ScopeType = scope?.Type,
            ScopeValue = scope?.Value,
            Status = ServiceStatus.Pending,
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        request.AddDomainEvent(new ServiceCreated(
            id,
            orderId,
            customerId,
            serviceType.Code,
            serviceType.Category,
            tier,
            scope?.Type.ToString(),
            scope?.Value,
            createdAt));

        return request;
    }

    public void AssignVendor(string vendorCode, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vendorCode);

        if (Status != ServiceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot assign vendor when status is {Status}.");
        }

        VendorCode = vendorCode;
        UpdatedAt = timestamp;
    }

    public void Dispatch(string? vendorReferenceId, DateTimeOffset dispatchedAt)
    {
        if (Status != ServiceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot dispatch when status is {Status}.");
        }

        if (string.IsNullOrWhiteSpace(VendorCode))
        {
            throw new InvalidOperationException("Cannot dispatch without vendor assignment.");
        }

        VendorReferenceId = vendorReferenceId;
        DispatchedAt = dispatchedAt;
        AttemptCount++;
        Status = ServiceStatus.Dispatched;
        UpdatedAt = dispatchedAt;

        AddDomainEvent(new ServiceDispatched(
            Id,
            OrderId,
            CustomerId,
            ServiceTypeCode,
            VendorCode!,
            vendorReferenceId,
            dispatchedAt));
    }

    public void MarkInProgress(DateTimeOffset timestamp)
    {
        if (Status != ServiceStatus.Dispatched)
        {
            return; // Idempotent - already moved past dispatched
        }

        Status = ServiceStatus.InProgress;
        UpdatedAt = timestamp;

        AddDomainEvent(new ServiceInProgress(
            Id,
            OrderId,
            ServiceTypeCode,
            VendorCode!,
            timestamp));
    }

    public void RecordResult(ServiceResult result, DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (IsTerminal)
        {
            throw new InvalidOperationException($"Cannot record result when status is {Status}.");
        }

        Result = result;
        CompletedAt = completedAt;
        Status = ServiceStatus.Completed;
        UpdatedAt = completedAt;
        LastError = null;

        AddDomainEvent(new ServiceCompleted(
            Id,
            OrderId,
            CustomerId,
            ServiceTypeCode,
            result.Status,
            result.Records.Count,
            completedAt));
    }

    public void Fail(string errorMessage, DateTimeOffset failedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        if (IsTerminal)
        {
            throw new InvalidOperationException($"Cannot fail when status is {Status}.");
        }

        LastError = errorMessage;
        FailedAt = failedAt;
        Status = ServiceStatus.Failed;
        UpdatedAt = failedAt;

        var willRetry = CanRetry;

        AddDomainEvent(new ServiceFailed(
            Id,
            OrderId,
            CustomerId,
            ServiceTypeCode,
            errorMessage,
            AttemptCount,
            MaxAttempts,
            willRetry,
            failedAt));
    }

    public void Retry(DateTimeOffset retriedAt)
    {
        if (!CanRetry)
        {
            throw new InvalidOperationException(
                $"Cannot retry: status={Status}, attempts={AttemptCount}/{MaxAttempts}.");
        }

        Status = ServiceStatus.Pending;
        FailedAt = null;
        LastError = null;
        UpdatedAt = retriedAt;

        AddDomainEvent(new ServiceRetried(
            Id,
            OrderId,
            ServiceTypeCode,
            AttemptCount,
            retriedAt));
    }

    public void Cancel(string reason, DateTimeOffset canceledAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (IsTerminal)
        {
            if (Status == ServiceStatus.Canceled)
            {
                return; // Idempotent
            }

            throw new InvalidOperationException($"Cannot cancel when status is {Status}.");
        }

        CanceledAt = canceledAt;
        Status = ServiceStatus.Canceled;
        UpdatedAt = canceledAt;

        AddDomainEvent(new ServiceCanceled(
            Id,
            OrderId,
            CustomerId,
            ServiceTypeCode,
            reason,
            canceledAt));
    }

    public static Service Rehydrate(
        UlidId id,
        UlidId orderId,
        UlidId customerId,
        UlidId? catalogSnapshotId,
        string serviceTypeCode,
        ServiceCategory category,
        int tier,
        ServiceScopeType? scopeType,
        string? scopeValue,
        ServiceStatus status,
        string? vendorCode,
        string? vendorReferenceId,
        int attemptCount,
        int maxAttempts,
        string? lastError,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        DateTimeOffset? dispatchedAt,
        DateTimeOffset? completedAt,
        DateTimeOffset? failedAt,
        DateTimeOffset? canceledAt,
        ServiceResult? result
    )
    {
        return new Service
        {
            Id = id,
            OrderId = orderId,
            CustomerId = customerId,
            CatalogSnapshotId = catalogSnapshotId,
            ServiceTypeCode = serviceTypeCode,
            Category = category,
            Tier = tier,
            ScopeType = scopeType,
            ScopeValue = scopeValue,
            Status = status,
            VendorCode = vendorCode,
            VendorReferenceId = vendorReferenceId,
            AttemptCount = attemptCount,
            MaxAttempts = maxAttempts,
            LastError = lastError,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DispatchedAt = dispatchedAt,
            CompletedAt = completedAt,
            FailedAt = failedAt,
            CanceledAt = canceledAt,
            Result = result
        };
    }

    public override string GetStreamId()
    {
        return $"{GetStreamType()}:{Id}";
    }

    public override string GetStreamType()
    {
        return "Service";
    }
}