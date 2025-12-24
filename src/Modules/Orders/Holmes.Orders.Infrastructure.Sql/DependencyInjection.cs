using Holmes.Orders.Application.Abstractions.Projections;
using Holmes.Orders.Application.Abstractions.Queries;
using Holmes.Orders.Domain;
using Holmes.Orders.Infrastructure.Sql.Projections;
using Holmes.Orders.Infrastructure.Sql.Queries;
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
        services.AddDbContext<WorkflowDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(WorkflowDbContext).Assembly.FullName)));

        services.AddScoped<IWorkflowUnitOfWork, WorkflowUnitOfWork>();
        services.AddScoped<IOrderTimelineWriter, SqlOrderTimelineWriter>();
        services.AddScoped<IOrderSummaryWriter, SqlOrderSummaryWriter>();
        services.AddScoped<IOrderQueries, SqlOrderQueries>();
        services.AddScoped<OrderSummaryProjectionRunner>();
        services.AddScoped<OrderTimelineProjectionRunner>();
        return services;
    }
}