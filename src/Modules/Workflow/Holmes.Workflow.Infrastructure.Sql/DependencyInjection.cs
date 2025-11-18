using Holmes.Workflow.Application.Abstractions.Projections;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Workflow.Infrastructure.Sql;

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
        services.AddScoped<OrderSummaryProjectionRunner>();
        services.AddScoped<OrderTimelineProjectionRunner>();
        return services;
    }
}