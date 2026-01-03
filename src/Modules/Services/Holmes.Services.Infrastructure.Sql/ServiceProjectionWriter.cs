using Holmes.Services.Contracts;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Services.Infrastructure.Sql;

public sealed class ServiceProjectionWriter(
    ServicesDbContext dbContext,
    ILogger<ServiceProjectionWriter> logger
) : IServiceProjectionWriter
{
    public async Task UpsertAsync(ServiceProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.ServiceProjections
            .FirstOrDefaultAsync(x => x.Id == model.ServiceId, cancellationToken);

        if (record is null)
        {
            record = new ServiceProjectionDb
            {
                Id = model.ServiceId
            };
            dbContext.ServiceProjections.Add(record);
        }

        record.OrderId = model.OrderId;
        record.CustomerId = model.CustomerId;
        record.ServiceTypeCode = model.ServiceTypeCode;
        record.Category = (int)model.Category;
        record.Status = (int)model.Status;
        record.Tier = model.Tier;
        record.ScopeType = model.ScopeType;
        record.ScopeValue = model.ScopeValue;
        record.CreatedAt = model.CreatedAt.UtcDateTime;
        record.UpdatedAt = model.CreatedAt.UtcDateTime;
        record.AttemptCount = 0;
        record.RecordCount = 0;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDispatchedAsync(
        string serviceId,
        string vendorCode,
        string? vendorReferenceId,
        DateTimeOffset dispatchedAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.ServiceProjections
            .FirstOrDefaultAsync(x => x.Id == serviceId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Service projection not found for dispatch update: {ServiceId}", serviceId);
            return;
        }

        record.Status = (int)ServiceStatus.Dispatched;
        record.VendorCode = vendorCode;
        record.VendorReferenceId = vendorReferenceId;
        record.DispatchedAt = dispatchedAt.UtcDateTime;
        record.AttemptCount++;
        record.UpdatedAt = dispatchedAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateInProgressAsync(
        string serviceId,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.ServiceProjections
            .FirstOrDefaultAsync(x => x.Id == serviceId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Service projection not found for in-progress update: {ServiceId}",
                serviceId);
            return;
        }

        record.Status = (int)ServiceStatus.InProgress;
        record.UpdatedAt = updatedAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCompletedAsync(
        string serviceId,
        ServiceResultStatus resultStatus,
        int recordCount,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.ServiceProjections
            .FirstOrDefaultAsync(x => x.Id == serviceId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Service projection not found for completion update: {ServiceId}",
                serviceId);
            return;
        }

        record.Status = (int)ServiceStatus.Completed;
        record.ResultStatus = (int)resultStatus;
        record.RecordCount = recordCount;
        record.CompletedAt = completedAt.UtcDateTime;
        record.LastError = null;
        record.UpdatedAt = completedAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateFailedAsync(
        string serviceId,
        string errorMessage,
        int attemptCount,
        bool willRetry,
        DateTimeOffset failedAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.ServiceProjections
            .FirstOrDefaultAsync(x => x.Id == serviceId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Service projection not found for failure update: {ServiceId}", serviceId);
            return;
        }

        record.Status = (int)ServiceStatus.Failed;
        record.LastError = errorMessage;
        record.AttemptCount = attemptCount;
        record.FailedAt = failedAt.UtcDateTime;
        record.UpdatedAt = failedAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCanceledAsync(
        string serviceId,
        string reason,
        DateTimeOffset canceledAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.ServiceProjections
            .FirstOrDefaultAsync(x => x.Id == serviceId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Service projection not found for cancel update: {ServiceId}", serviceId);
            return;
        }

        record.Status = (int)ServiceStatus.Canceled;
        record.CancelReason = reason;
        record.CanceledAt = canceledAt.UtcDateTime;
        record.UpdatedAt = canceledAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRetriedAsync(
        string serviceId,
        int attemptCount,
        DateTimeOffset retriedAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.ServiceProjections
            .FirstOrDefaultAsync(x => x.Id == serviceId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Service projection not found for retry update: {ServiceId}", serviceId);
            return;
        }

        record.Status = (int)ServiceStatus.Pending;
        record.AttemptCount = attemptCount;
        record.LastError = null;
        record.FailedAt = null;
        record.UpdatedAt = retriedAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}