using Microsoft.EntityFrameworkCore;

namespace Holmes.Core.Infrastructure.Sql;

public class EntityStore<TEntity>(DbContext dbContext)
    where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet = dbContext.Set<TEntity>();

    public async Task CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }
}