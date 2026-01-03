using Holmes.Core.Domain.Specifications;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class ServiceByVendorReferenceSpec : Specification<ServiceDb>
{
    public ServiceByVendorReferenceSpec(string vendorCode, string vendorReferenceId)
    {
        AddCriteria(r => r.VendorCode == vendorCode && r.VendorReferenceId == vendorReferenceId);
        AddInclude(r => r.Result!);
    }
}