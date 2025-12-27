using Holmes.Core.Domain.Specifications;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class ServiceByIdSpec : Specification<ServiceDb>
{
    public ServiceByIdSpec(string id)
    {
        AddCriteria(r => r.Id == id);
        AddInclude(r => r.Result!);
    }
}