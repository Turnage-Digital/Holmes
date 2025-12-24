using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Mappers;
using Holmes.Services.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Services.Infrastructure.Sql;

public class ServiceRepository(ServicesDbContext context) 
    : IServiceRepository
{
    public async Task<Service?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var idStr = id.ToString();
        var spec = new ServiceByIdSpec(idStr);

        var db = await context.Services
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return db is null ? null : ServiceMapper.ToDomain(db);
    }

    public async Task<Service?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new ServiceByVendorReferenceSpec(vendorCode, vendorReferenceId);

        var db = await context.Services
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return db is null ? null : ServiceMapper.ToDomain(db);
    }

    public void Add(Service service)
    {
        var db = ServiceMapper.ToDb(service);
        context.Services.Add(db);
    }

    public void Update(Service service)
    {
        var idStr = service.Id.ToString();
        var db = context.Services.Local.FirstOrDefault(r => r.Id == idStr);

        if (db is null)
        {
            db = ServiceMapper.ToDb(service);
            context.Services.Attach(db);
            context.Entry(db).State = EntityState.Modified;
        }
        else
        {
            ServiceMapper.UpdateDb(db, service);
        }
    }
}
