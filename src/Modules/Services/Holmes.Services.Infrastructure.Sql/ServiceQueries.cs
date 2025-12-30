using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Services.Contracts.Dtos;
using Holmes.Services.Application.Queries;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Services.Infrastructure.Sql;

public sealed class ServiceQueries(ServicesDbContext dbContext) : IServiceQueries
{
    public async Task<ServiceSummaryDto?> GetByIdAsync(
        string serviceId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ServiceByIdSpec(serviceId);

        return await dbContext.Services
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceSummaryDto(
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

    public async Task<IReadOnlyList<ServiceSummaryDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ServicesByOrderIdSpec(orderId);

        return await dbContext.Services
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceSummaryDto(
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

    public async Task<IReadOnlyList<ServiceDispatchDto>> GetPendingByTierAsync(
        string orderId,
        int tier,
        CancellationToken cancellationToken
    )
    {
        var spec = new PendingServicesByTierSpec(orderId, tier);

        return await dbContext.Services
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceDispatchDto(
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

    public async Task<IReadOnlyList<ServiceDispatchDto>> GetPendingForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var spec = new PendingServicesForDispatchSpec(batchSize);

        return await dbContext.Services
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceDispatchDto(
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

    public async Task<IReadOnlyList<ServiceDispatchDto>> GetRetryableAsync(
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var spec = new RetryableServicesSpec(batchSize);

        return await dbContext.Services
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceDispatchDto(
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

    public async Task<ServiceDispatchDto?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ServiceByVendorReferenceSpec(vendorCode, vendorReferenceId);

        return await dbContext.Services
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(s => new ServiceDispatchDto(
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
        return await dbContext.Services
            .AsNoTracking()
            .Where(r => r.OrderId == orderId)
            .AllAsync(r => r.Status == ServiceStatus.Completed || r.Status == ServiceStatus.Canceled,
                cancellationToken);
    }

    public async Task<bool> TierCompletedAsync(string orderId, int tier, CancellationToken cancellationToken)
    {
        return await dbContext.Services
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
        var services = await dbContext.Services
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

    public async Task<ServiceFulfillmentQueuePagedResult> GetFulfillmentQueuePagedAsync(
        ServiceFulfillmentQueueFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.Services.AsNoTracking().AsQueryable();

        // Default to pending and in-progress statuses for the fulfillment queue
        if (filter.Statuses is not null && filter.Statuses.Count > 0)
        {
            query = query.Where(s => filter.Statuses.Contains(s.Status));
        }
        else
        {
            // Default: show pending, dispatched, and in-progress for fulfillment queue
            query = query.Where(s =>
                s.Status == ServiceStatus.Pending ||
                s.Status == ServiceStatus.Dispatched ||
                s.Status == ServiceStatus.InProgress);
        }

        // Apply customer filter
        if (filter.CustomerId is not null)
        {
            query = query.Where(s => s.CustomerId == filter.CustomerId);
        }
        else if (filter.AllowedCustomerIds is not null && filter.AllowedCustomerIds.Count > 0)
        {
            query = query.Where(s => filter.AllowedCustomerIds.Contains(s.CustomerId));
        }

        // Apply category filter
        if (filter.Categories is not null && filter.Categories.Count > 0)
        {
            query = query.Where(s => filter.Categories.Contains(s.Category));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.CreatedAt)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ServiceSummaryDto(
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

        return new ServiceFulfillmentQueuePagedResult(items, totalCount);
    }
}