using Holmes.Notifications.Contracts;
using Holmes.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Notifications.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion version
    )
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseMySql(connectionString, version));

        // Write side
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();

        // Read side (CQRS)
        services.AddScoped<INotificationQueries, NotificationQueries>();

        // Projections
        services.AddScoped<INotificationProjectionWriter, NotificationProjectionWriter>();
        services.AddScoped<NotificationEventProjectionRunner>();

        // Stub providers - replace these with real implementations when ready
        services.AddScoped<INotificationProvider, LoggingEmailProvider>();
        services.AddScoped<INotificationProvider, LoggingSmsProvider>();
        services.AddScoped<INotificationProvider, LoggingWebhookProvider>();

        return services;
    }
}