using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Entities;
using Holmes.Services.Infrastructure.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Services.Infrastructure.Sql;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly ServicesDbContext _context;
    private readonly Dictionary<string, ServiceRequest> _tracked = new();

    public ServiceRequestRepository(ServicesDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceRequest?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var idStr = id.ToString();

        if (_tracked.TryGetValue(idStr, out var tracked))
        {
            return tracked;
        }

        var db = await _context.ServiceRequests
            .Include(r => r.Result)
            .FirstOrDefaultAsync(r => r.Id == idStr, cancellationToken);

        if (db is null)
        {
            return null;
        }

        var domain = ServiceRequestMapper.ToDomain(db);
        _tracked[idStr] = domain;
        return domain;
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetByOrderIdAsync(UlidId orderId, CancellationToken cancellationToken = default)
    {
        var orderIdStr = orderId.ToString();

        var dbs = await _context.ServiceRequests
            .Include(r => r.Result)
            .Where(r => r.OrderId == orderIdStr)
            .ToListAsync(cancellationToken);

        return dbs.Select(db =>
        {
            if (_tracked.TryGetValue(db.Id, out var tracked))
            {
                return tracked;
            }

            var domain = ServiceRequestMapper.ToDomain(db);
            _tracked[db.Id] = domain;
            return domain;
        }).ToList();
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetPendingByTierAsync(
        UlidId orderId,
        int tier,
        CancellationToken cancellationToken = default)
    {
        var orderIdStr = orderId.ToString();

        var dbs = await _context.ServiceRequests
            .Include(r => r.Result)
            .Where(r => r.OrderId == orderIdStr && r.Tier == tier && r.Status == ServiceStatus.Pending)
            .ToListAsync(cancellationToken);

        return dbs.Select(db =>
        {
            if (_tracked.TryGetValue(db.Id, out var tracked))
            {
                return tracked;
            }

            var domain = ServiceRequestMapper.ToDomain(db);
            _tracked[db.Id] = domain;
            return domain;
        }).ToList();
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetPendingForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var dbs = await _context.ServiceRequests
            .Where(r => r.Status == ServiceStatus.Pending && r.VendorCode != null)
            .OrderBy(r => r.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return dbs.Select(db =>
        {
            if (_tracked.TryGetValue(db.Id, out var tracked))
            {
                return tracked;
            }

            var domain = ServiceRequestMapper.ToDomain(db);
            _tracked[db.Id] = domain;
            return domain;
        }).ToList();
    }

    public async Task<IReadOnlyList<ServiceRequest>> GetRetryableAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        var dbs = await _context.ServiceRequests
            .Where(r => r.Status == ServiceStatus.Failed && r.AttemptCount < r.MaxAttempts)
            .OrderBy(r => r.FailedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return dbs.Select(db =>
        {
            if (_tracked.TryGetValue(db.Id, out var tracked))
            {
                return tracked;
            }

            var domain = ServiceRequestMapper.ToDomain(db);
            _tracked[db.Id] = domain;
            return domain;
        }).ToList();
    }

    public async Task<ServiceRequest?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken = default)
    {
        var db = await _context.ServiceRequests
            .Include(r => r.Result)
            .FirstOrDefaultAsync(
                r => r.VendorCode == vendorCode && r.VendorReferenceId == vendorReferenceId,
                cancellationToken);

        if (db is null)
        {
            return null;
        }

        if (_tracked.TryGetValue(db.Id, out var tracked))
        {
            return tracked;
        }

        var domain = ServiceRequestMapper.ToDomain(db);
        _tracked[db.Id] = domain;
        return domain;
    }

    public async Task<bool> AllCompletedForOrderAsync(UlidId orderId, CancellationToken cancellationToken = default)
    {
        var orderIdStr = orderId.ToString();

        return await _context.ServiceRequests
            .Where(r => r.OrderId == orderIdStr)
            .AllAsync(r => r.Status == ServiceStatus.Completed || r.Status == ServiceStatus.Canceled, cancellationToken);
    }

    public async Task<bool> TierCompletedAsync(UlidId orderId, int tier, CancellationToken cancellationToken = default)
    {
        var orderIdStr = orderId.ToString();

        return await _context.ServiceRequests
            .Where(r => r.OrderId == orderIdStr && r.Tier == tier)
            .AllAsync(r => r.Status == ServiceStatus.Completed || r.Status == ServiceStatus.Canceled, cancellationToken);
    }

    public void Add(ServiceRequest request)
    {
        var db = ServiceRequestMapper.ToDb(request);
        _context.ServiceRequests.Add(db);
        _tracked[request.Id.ToString()] = request;
    }

    public void Update(ServiceRequest request)
    {
        var idStr = request.Id.ToString();
        var db = _context.ServiceRequests.Local.FirstOrDefault(r => r.Id == idStr);

        if (db is null)
        {
            db = ServiceRequestMapper.ToDb(request);
            _context.ServiceRequests.Attach(db);
            _context.Entry(db).State = EntityState.Modified;
        }
        else
        {
            ServiceRequestMapper.UpdateDb(db, request);
        }

        _tracked[idStr] = request;
    }
}
