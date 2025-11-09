using Holmes.Customers.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Customers.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddCustomersInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<CustomersDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(CustomersDbContext).Assembly.FullName)));

        services.AddScoped<ICustomersUnitOfWork, CustomersUnitOfWork>();

        return services;
    }
}
