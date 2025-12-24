using Holmes.Core.Domain.Specifications;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Entities;

namespace Holmes.Services.Infrastructure.Sql.Specifications;

public sealed class RetryableServicesSpec : Specification<ServiceDb>
{
    public RetryableServicesSpec(int batchSize)
    {
        AddCriteria(r => r.Status == ServiceStatus.Failed && r.AttemptCount < r.MaxAttempts);
        ApplyOrderBy(r => r.FailedAt!);
        ApplyPaging(0, batchSize);
    }
}