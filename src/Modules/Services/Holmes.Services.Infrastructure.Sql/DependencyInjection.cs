using Holmes.Services.Application.Queries;
using Holmes.Services.Contracts;
using Holmes.Services.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Services.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddServicesInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<ServicesDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(ServicesDbContext).Assembly.FullName)));

        services.AddScoped<ServiceRepository>();
        services.AddScoped<IServiceRepository>(sp => sp.GetRequiredService<ServiceRepository>());
        services.AddScoped<IServicesUnitOfWork, ServicesUnitOfWork>();
        services.AddScoped<IServiceQueries, ServiceQueries>();
        services.AddScoped<IServiceCatalogQueries, ServiceCatalogQueries>();
        services.AddScoped<IServiceCatalogRepository, ServiceCatalogRepository>();

        // Projection writer
        services.AddScoped<IServiceProjectionWriter, ServiceProjectionWriter>();

        // Vendor adapters
        services.AddSingleton<IVendorAdapter, StubVendorAdapter>();
        services.AddSingleton<IVendorAdapterFactory, VendorAdapterFactory>();
        services.AddSingleton<IVendorCredentialStore, InMemoryVendorCredentialStore>();

        // SSE broadcaster for service status changes
        services.AddSingleton<IServiceChangeBroadcaster, ServiceChangeBroadcaster>();

        return services;
    }
}