using Holmes.Notifications.Application.Abstractions.Projections;
using Holmes.Notifications.Application.Abstractions.Queries;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql.Projections;
using Holmes.Notifications.Infrastructure.Sql.Queries;
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
        services.AddScoped<INotificationRequestRepository, NotificationRequestRepository>();
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();

        // Read side (CQRS)
        services.AddScoped<INotificationQueries, SqlNotificationQueries>();

        // Projections
        services.AddScoped<INotificationProjectionWriter, SqlNotificationProjectionWriter>();
        services.AddScoped<NotificationEventProjectionRunner>();

        // Stub providers - replace these with real implementations when ready
        services.AddScoped<INotificationProvider, LoggingEmailProvider>();
        services.AddScoped<INotificationProvider, LoggingSmsProvider>();
        services.AddScoped<INotificationProvider, LoggingWebhookProvider>();

        return services;
    }
}