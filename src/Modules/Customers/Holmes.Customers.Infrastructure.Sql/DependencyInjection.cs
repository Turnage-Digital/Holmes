using Holmes.Customers.Application.Abstractions.Queries;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql.Queries;
using Holmes.Customers.Infrastructure.Sql.Repositories;
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
        services.AddScoped<ICustomerAccessQueries, SqlCustomerAccessQueries>();
        services.AddScoped<ICustomerQueries, SqlCustomerQueries>();
        services.AddScoped<ICustomerProfileRepository, SqlCustomerProfileRepository>();

        return services;
    }
}