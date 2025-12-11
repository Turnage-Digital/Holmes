using Holmes.Intake.Application.Abstractions.Projections;
using Holmes.Intake.Application.Abstractions.Services;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.Storage;
using Holmes.Intake.Infrastructure.Sql.Projections;
using Holmes.Intake.Infrastructure.Sql.Services;
using Holmes.Intake.Infrastructure.Sql.Storage;
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
        services.AddScoped<IIntakeSessionReplaySource, SqlIntakeSessionReplaySource>();
        services.AddScoped<IntakeSessionProjectionRunner>();
        services.AddScoped<IIntakeAnswersDecryptor, IntakeAnswersDecryptor>();

        return services;
    }
}