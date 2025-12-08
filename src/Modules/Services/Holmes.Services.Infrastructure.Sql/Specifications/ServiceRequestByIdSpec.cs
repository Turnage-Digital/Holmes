using Holmes.Core.Domain.Specifications;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class ServiceRequestByIdSpec : Specification<ServiceRequestDb>
{
    public ServiceRequestByIdSpec(string id)
    {
        AddCriteria(r => r.Id == id);
        AddInclude(r => r.Result!);
    }
}