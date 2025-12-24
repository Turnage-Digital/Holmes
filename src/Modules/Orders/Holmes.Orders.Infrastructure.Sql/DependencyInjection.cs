using Holmes.Orders.Application.Abstractions;
using Holmes.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Orders.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkflowInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(OrdersDbContext).Assembly.FullName)));

        services.AddScoped<IWorkflowUnitOfWork, OrdersUnitOfWork>();
        services.AddScoped<IOrderTimelineWriter, OrderTimelineWriter>();
        services.AddScoped<IOrderSummaryWriter, OrderSummaryWriter>();
        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddScoped<OrderSummaryProjectionRunner>();
        services.AddScoped<OrderTimelineProjectionRunner>();
        return services;
    }
}