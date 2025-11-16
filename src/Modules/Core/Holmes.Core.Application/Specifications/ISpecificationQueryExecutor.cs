using Holmes.Core.Domain.Specifications;

namespace Holmes.Core.Application.Specifications;

public interface ISpecificationQueryExecutor
{
    IQueryable<T> Apply<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class;
}