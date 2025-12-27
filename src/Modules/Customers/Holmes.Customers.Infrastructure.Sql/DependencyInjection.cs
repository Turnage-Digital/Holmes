using Holmes.Customers.Application.Abstractions;
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
        services.AddScoped<ICustomerAccessQueries, CustomerAccessQueries>();
        services.AddScoped<ICustomerQueries, CustomerQueries>();
        services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
        services.AddScoped<ICustomerProjectionWriter, CustomerProjectionWriter>();

        return services;
    }
}