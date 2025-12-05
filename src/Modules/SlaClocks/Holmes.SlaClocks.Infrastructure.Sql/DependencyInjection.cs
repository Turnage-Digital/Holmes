using Holmes.SlaClocks.Application.Services;
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
        services.AddDbContext<SlaClockDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(SlaClockDbContext).Assembly.FullName)));

        services.AddScoped<SlaClockRepository>();
        services.AddScoped<ISlaClockRepository>(sp => sp.GetRequiredService<SlaClockRepository>());
        services.AddScoped<ISlaClockUnitOfWork, SlaClockUnitOfWork>();
        services.AddScoped<IBusinessCalendarService, BusinessCalendarService>();

        return services;
    }
}
