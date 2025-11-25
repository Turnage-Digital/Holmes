using Holmes.Core.Application.Abstractions.Specifications;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Core.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        // services.AddSingleton<IAeadEncryptor, NoOpAeadEncryptor>();

        services.AddDbContext<CoreDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(CoreDbContext).Assembly.FullName)));

        services.AddSingleton<ISpecificationQueryExecutor, EfSpecificationQueryExecutor>();

        return services;
    }
}