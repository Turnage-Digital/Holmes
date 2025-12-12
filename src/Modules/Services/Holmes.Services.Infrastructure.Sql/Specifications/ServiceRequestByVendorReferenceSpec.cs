using Holmes.Core.Domain.Specifications;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class ServiceRequestByVendorReferenceSpec : Specification<ServiceRequestDb>
{
    public ServiceRequestByVendorReferenceSpec(string vendorCode, string vendorReferenceId)
    {
        AddCriteria(r => r.VendorCode == vendorCode && r.VendorReferenceId == vendorReferenceId);
        AddInclude(r => r.Result!);
    }
}