using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Services.Infrastructure.Sql.Queries;

public sealed class SqlServiceRequestQueries(ServicesDbContext dbContext) : IServiceRequestQueries
{
    public async Task<ServiceRequestSummaryDto?> GetByIdAsync(
        string serviceRequestId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ServiceRequestByIdSpec(serviceRequestId);

        return await dbContext.ServiceRequests
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceRequestSummaryDto(
                s.Id,
                s.OrderId,
                s.CustomerId,
                s.ServiceTypeCode,
                s.Category,
                s.Tier,
                s.Status,
                s.VendorCode,
                s.VendorReferenceId,
                s.AttemptCount,
                s.MaxAttempts,
                s.LastError,
                s.ScopeType,
                s.ScopeValue,
                new DateTimeOffset(s.CreatedAt, TimeSpan.Zero),
                s.DispatchedAt.HasValue ? new DateTimeOffset(s.DispatchedAt.Value, TimeSpan.Zero) : null,
                s.CompletedAt.HasValue ? new DateTimeOffset(s.CompletedAt.Value, TimeSpan.Zero) : null,
                s.FailedAt.HasValue ? new DateTimeOffset(s.FailedAt.Value, TimeSpan.Zero) : null,
                s.CanceledAt.HasValue ? new DateTimeOffset(s.CanceledAt.Value, TimeSpan.Zero) : null
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequestSummaryDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ServiceRequestsByOrderIdSpec(orderId);

        return await dbContext.ServiceRequests
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceRequestSummaryDto(
                s.Id,
                s.OrderId,
                s.CustomerId,
                s.ServiceTypeCode,
                s.Category,
                s.Tier,
                s.Status,
                s.VendorCode,
                s.VendorReferenceId,
                s.AttemptCount,
                s.MaxAttempts,
                s.LastError,
                s.ScopeType,
                s.ScopeValue,
                new DateTimeOffset(s.CreatedAt, TimeSpan.Zero),
                s.DispatchedAt.HasValue ? new DateTimeOffset(s.DispatchedAt.Value, TimeSpan.Zero) : null,
                s.CompletedAt.HasValue ? new DateTimeOffset(s.CompletedAt.Value, TimeSpan.Zero) : null,
                s.FailedAt.HasValue ? new DateTimeOffset(s.FailedAt.Value, TimeSpan.Zero) : null,
                s.CanceledAt.HasValue ? new DateTimeOffset(s.CanceledAt.Value, TimeSpan.Zero) : null
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequestDispatchDto>> GetPendingByTierAsync(
        string orderId,
        int tier,
        CancellationToken cancellationToken
    )
    {
        var spec = new PendingServiceRequestsByTierSpec(orderId, tier);

        return await dbContext.ServiceRequests
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceRequestDispatchDto(
                s.Id,
                s.OrderId,
                s.CustomerId,
                s.ServiceTypeCode,
                s.Tier,
                s.VendorCode,
                s.Status,
                s.AttemptCount,
                s.MaxAttempts
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequestDispatchDto>> GetPendingForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var spec = new PendingServiceRequestsForDispatchSpec(batchSize);

        return await dbContext.ServiceRequests
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceRequestDispatchDto(
                s.Id,
                s.OrderId,
                s.CustomerId,
                s.ServiceTypeCode,
                s.Tier,
                s.VendorCode,
                s.Status,
                s.AttemptCount,
                s.MaxAttempts
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequestDispatchDto>> GetRetryableAsync(
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var spec = new RetryableServiceRequestsSpec(batchSize);

        return await dbContext.ServiceRequests
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceRequestDispatchDto(
                s.Id,
                s.OrderId,
                s.CustomerId,
                s.ServiceTypeCode,
                s.Tier,
                s.VendorCode,
                s.Status,
                s.AttemptCount,
                s.MaxAttempts
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceRequestDispatchDto?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ServiceRequestByVendorReferenceSpec(vendorCode, vendorReferenceId);

        return await dbContext.ServiceRequests
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceRequestDispatchDto(
                s.Id,
                s.OrderId,
                s.CustomerId,
                s.ServiceTypeCode,
                s.Tier,
                s.VendorCode,
                s.Status,
                s.AttemptCount,
                s.MaxAttempts
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> AllCompletedForOrderAsync(string orderId, CancellationToken cancellationToken)
    {
        return await dbContext.ServiceRequests
            .AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .AllAsync(r => r.Status == ServiceStatus.Completed || r.Status == ServiceStatus.Canceled,
                cancellationToken);
    }

    public async Task<bool> TierCompletedAsync(string orderId, int tier, CancellationToken cancellationToken)
    {
        return await dbContext.ServiceRequests
            .AsNoTracking()
            .Where(r => r.OrderId == orderId && r.Tier == tier)
            .AllAsync(r => r.Status == ServiceStatus.Completed || r.Status == ServiceStatus.Canceled,
                cancellationToken);
    }

    public async Task<OrderCompletionStatusDto> GetOrderCompletionStatusAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        var services = await dbContext.ServiceRequests
            .AsNoTracking()
            .Where(s => s.OrderId == orderId)
            .Select(s => s.Status)
            .ToListAsync(cancellationToken);

        var totalServices = services.Count;
        var completedServices = services.Count(s => s == ServiceStatus.Completed);
        var pendingServices = services.Count(s => s == ServiceStatus.Pending);
        var failedServices = services.Count(s => s == ServiceStatus.Failed);
        var canceledServices = services.Count(s => s == ServiceStatus.Canceled);
        var allCompleted = services.All(s => s == ServiceStatus.Completed || s == ServiceStatus.Canceled);

        return new OrderCompletionStatusDto(
            orderId,
            totalServices,
            completedServices,
            pendingServices,
            failedServices,
            canceledServices,
            allCompleted
        );
    }
}