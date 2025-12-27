using Holmes.SlaClocks.Application.Abstractions;
using Holmes.SlaClocks.Application.Abstractions.Services;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.SlaClocks.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddSlaClockInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<SlaClocksDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(SlaClocksDbContext).Assembly.FullName)));

        // Write side
        services.AddScoped<SlaClockRepository>();
        services.AddScoped<ISlaClockRepository>(sp => sp.GetRequiredService<SlaClockRepository>());
        services.AddScoped<ISlaClocksUnitOfWork, SlaClocksUnitOfWork>();

        // Read side (CQRS)
        services.AddScoped<ISlaClockQueries, SlaClockQueries>();

        // Projections
        services.AddScoped<ISlaClockProjectionWriter, SlaClockProjectionWriter>();
        services.AddScoped<SlaClockEventProjectionRunner>();

        // Services
        services.AddScoped<IBusinessCalendarService, BusinessCalendarService>();

        // SSE Broadcaster (singleton for in-memory pub/sub)
        services.AddSingleton<ISlaClockChangeBroadcaster, SlaClockChangeBroadcaster>();

        return services;
    }
}