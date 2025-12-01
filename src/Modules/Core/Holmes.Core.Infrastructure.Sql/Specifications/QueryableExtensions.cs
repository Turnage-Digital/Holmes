using Holmes.Core.Domain.Specifications;

namespace Holmes.Core.Infrastructure.Sql.Specifications;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySpecification<T>(
        this IQueryable<T> query,
        ISpecification<T> specification
    )
        where T : class
    {
        return SpecificationEvaluator.GetQuery(query, specification);
    }
}