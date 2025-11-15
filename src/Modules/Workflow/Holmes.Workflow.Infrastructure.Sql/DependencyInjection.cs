using Holmes.Workflow.Domain;
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
        return services;
    }
}