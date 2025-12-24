using Holmes.IntakeSessions.Application.Abstractions;
using Holmes.IntakeSessions.Application.Abstractions.Services;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.IntakeSessions.Infrastructure.Sql;

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

        // Write side
        services.AddScoped<IIntakeUnitOfWork, IntakeUnitOfWork>();
        services.AddScoped<IConsentArtifactStore, DatabaseConsentArtifactStore>();

        // Read side (CQRS)
        services.AddScoped<IIntakeSessionQueries, IntakeSessionQueries>();

        // Projections
        services.AddScoped<IIntakeSessionProjectionWriter, IntakeSessionProjectionWriter>();
        services.AddScoped<IIntakeSessionReplaySource, IntakeSessionReplaySource>();
        services.AddScoped<IntakeSessionProjectionRunner>();

        // Services
        services.AddScoped<IIntakeAnswersDecryptor, IntakeAnswersDecryptor>();

        return services;
    }
}