using Holmes.Core.Application.Specifications;
using Holmes.Core.Domain.Specifications;

namespace Holmes.Core.Infrastructure.Sql.Specifications;

public sealed class EfSpecificationQueryExecutor : ISpecificationQueryExecutor
{
    public IQueryable<T> Apply<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        return SpecificationEvaluator.GetQuery(query, specification);
    }
}