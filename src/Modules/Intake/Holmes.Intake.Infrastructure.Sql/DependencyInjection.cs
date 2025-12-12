using Holmes.Intake.Application.Abstractions.Projections;
using Holmes.Intake.Application.Abstractions.Queries;
using Holmes.Intake.Application.Abstractions.Services;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql.Projections;
using Holmes.Intake.Infrastructure.Sql.Queries;
using Holmes.Intake.Infrastructure.Sql.Services;
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

        // Write side
        services.AddScoped<IIntakeUnitOfWork, IntakeUnitOfWork>();
        services.AddScoped<IConsentArtifactStore, DatabaseConsentArtifactStore>();

        // Read side (CQRS)
        services.AddScoped<IIntakeSessionQueries, SqlIntakeSessionQueries>();

        // Projections
        services.AddScoped<IIntakeSessionProjectionWriter, SqlIntakeSessionProjectionWriter>();
        services.AddScoped<IIntakeSessionReplaySource, SqlIntakeSessionReplaySource>();
        services.AddScoped<IntakeSessionProjectionRunner>();

        // Services
        services.AddScoped<IIntakeAnswersDecryptor, IntakeAnswersDecryptor>();

        return services;
    }
}