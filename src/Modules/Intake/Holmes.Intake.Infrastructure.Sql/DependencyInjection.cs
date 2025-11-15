using Holmes.Intake.Application.Projections;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.Storage;
using Holmes.Intake.Infrastructure.Sql.Storage;
using Holmes.Intake.Infrastructure.Sql.Projections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Intake.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddIntakeInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<IntakeDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(IntakeDbContext).Assembly.FullName)));

        services.AddScoped<IIntakeUnitOfWork, IntakeUnitOfWork>();
        services.AddScoped<IConsentArtifactStore, DatabaseConsentArtifactStore>();
        services.AddScoped<IIntakeSessionProjectionWriter, SqlIntakeSessionProjectionWriter>();

        return services;
    }
}
