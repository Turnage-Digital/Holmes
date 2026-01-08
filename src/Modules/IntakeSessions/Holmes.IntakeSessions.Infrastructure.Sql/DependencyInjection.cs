using Holmes.IntakeSessions.Contracts;
using Holmes.IntakeSessions.Contracts.Services;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddIntakeSessionsInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<IntakeSessionsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(IntakeSessionsDbContext).Assembly.FullName)));

        // Write side
        services.AddScoped<IIntakeSessionsUnitOfWork, IntakeSessionsUnitOfWork>();
        services.AddScoped<IAuthorizationArtifactStore, DatabaseAuthorizationArtifactStore>();

        // Read side (CQRS)
        services.AddScoped<IIntakeSessionQueries, IntakeSessionQueries>();

        // Projections
        services.AddScoped<IIntakeSessionProjectionWriter, IntakeSessionProjectionWriter>();

        // Services
        services.AddScoped<IIntakeAnswersDecryptor, IntakeAnswersDecryptor>();

        return services;
    }
}
