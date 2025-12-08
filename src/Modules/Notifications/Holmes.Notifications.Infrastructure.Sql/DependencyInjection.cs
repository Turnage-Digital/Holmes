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

        services.AddScoped<INotificationRequestRepository, NotificationRequestRepository>();
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();

        // Stub providers - replace these with real implementations when ready
        services.AddScoped<INotificationProvider, LoggingEmailProvider>();
        services.AddScoped<INotificationProvider, LoggingSmsProvider>();
        services.AddScoped<INotificationProvider, LoggingWebhookProvider>();

        return services;
    }
}