using Holmes.Services.Application.Abstractions.Notifications;
using Holmes.Services.Application.Vendors;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql.Notifications;
using Holmes.Services.Infrastructure.Sql.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Services.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddServicesInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion)
    {
        services.AddDbContext<ServicesDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(ServicesDbContext).Assembly.FullName)));

        services.AddScoped<ServiceRequestRepository>();
        services.AddScoped<IServiceRequestRepository>(sp => sp.GetRequiredService<ServiceRequestRepository>());
        services.AddScoped<IServicesUnitOfWork, ServicesUnitOfWork>();

        // Vendor adapters
        services.AddSingleton<IVendorAdapter, StubVendorAdapter>();
        services.AddSingleton<IVendorAdapterFactory, VendorAdapterFactory>();
        services.AddSingleton<IVendorCredentialStore, InMemoryVendorCredentialStore>();

        // SSE broadcaster for service status changes
        services.AddSingleton<IServiceChangeBroadcaster, ServiceChangeBroadcaster>();

        return services;
    }
}
