using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Mappers;
using Holmes.Services.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Services.Infrastructure.Sql;

public class ServiceRequestRepository : IServiceRequestRepository
{
    private readonly ServicesDbContext _context;
    private readonly Dictionary<string, Service> _tracked = new();

    public ServiceRequestRepository(ServicesDbContext context)
    {
        _context = context;
    }

    public async Task<Service?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var idStr = id.ToString();

        if (_tracked.TryGetValue(idStr, out var tracked))
        {
            return tracked;
        }

        var spec = new ServiceRequestByIdSpec(idStr);

        var db = await _context.ServiceRequests
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (db is null)
        {
            return null;
        }

        var domain = ServiceRequestMapper.ToDomain(db);
        _tracked[idStr] = domain;
        return domain;
    }

    public async Task<IReadOnlyList<Service>> GetByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new ServiceRequestsByOrderIdSpec(orderId.ToString());

        var dbs = await _context.ServiceRequests
            .ApplySpecification(spec)
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
            })
            .ToList();
    }

    public async Task<IReadOnlyList<Service>> GetPendingByTierAsync(
        UlidId orderId,
        int tier,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new PendingServiceRequestsByTierSpec(orderId.ToString(), tier);

        var dbs = await _context.ServiceRequests
            .ApplySpecification(spec)
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
            })
            .ToList();
    }

    public async Task<IReadOnlyList<Service>> GetPendingForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new PendingServiceRequestsForDispatchSpec(batchSize);

        var dbs = await _context.ServiceRequests
            .ApplySpecification(spec)
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
            })
            .ToList();
    }

    public async Task<IReadOnlyList<Service>> GetRetryableAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new RetryableServiceRequestsSpec(batchSize);

        var dbs = await _context.ServiceRequests
            .ApplySpecification(spec)
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
            })
            .ToList();
    }

    public async Task<Service?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new ServiceRequestByVendorReferenceSpec(vendorCode, vendorReferenceId);

        var db = await _context.ServiceRequests
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

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
            .AllAsync(r => r.Status == ServiceStatus.Completed || r.Status == ServiceStatus.Canceled,
                cancellationToken);
    }

    public async Task<bool> TierCompletedAsync(UlidId orderId, int tier, CancellationToken cancellationToken = default)
    {
        var orderIdStr = orderId.ToString();

        return await _context.ServiceRequests
            .Where(r => r.OrderId == orderIdStr && r.Tier == tier)
            .AllAsync(r => r.Status == ServiceStatus.Completed || r.Status == ServiceStatus.Canceled,
                cancellationToken);
    }

    public void Add(Service request)
    {
        var db = ServiceRequestMapper.ToDb(request);
        _context.ServiceRequests.Add(db);
        _tracked[request.Id.ToString()] = request;
    }

    public void Update(Service request)
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