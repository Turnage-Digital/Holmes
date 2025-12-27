using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.App.Application;
using Holmes.App.Application.EventHandlers;
using Holmes.App.Server.Services;
using Holmes.Core.Application;
using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Application.Abstractions.Security;
using Holmes.Core.Application.Behaviors;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Security;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Events;
using Holmes.Customers.Application.Abstractions;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.IntakeSessions.Application.Abstractions;
using Holmes.IntakeSessions.Application.Abstractions.Services;
using Holmes.IntakeSessions.Application.Commands;
using Holmes.IntakeSessions.Application.Services;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Infrastructure.Sql;
using Holmes.Notifications.Application.Abstractions;
using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql;
using Holmes.Orders.Application.Abstractions;
using Holmes.Orders.Application.Commands;
using Holmes.Orders.Domain;
using Holmes.Orders.Infrastructure.Sql;
using Holmes.Services.Application.Abstractions;
using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Application.Commands;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql;
using Holmes.SlaClocks.Application.Abstractions;
using Holmes.SlaClocks.Application.Abstractions.Services;
using Holmes.SlaClocks.Application.Commands;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql;
using Holmes.SlaClocks.Infrastructure.Sql.Services;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Users.Application.Abstractions;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Holmes.App.Server;

internal static class DependencyInjection
{
    public static IServiceCollection AddHolmesObservability(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddHealthChecks();
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                "Holmes.App.Server",
                serviceVersion: typeof(DependencyInjection).Assembly.GetName().Version?.ToString()))
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(UnitOfWorkTelemetry.MeterName)
                    .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(UnitOfWorkTelemetry.ActivitySourceName);

                var otlpEndpoint = configuration["OpenTelemetry:Exporter:Endpoint"];
                tracing.AddOtlpExporter(options =>
                {
                    if (!string.IsNullOrWhiteSpace(otlpEndpoint) &&
                        Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
                    {
                        options.Endpoint = endpoint;
                    }
                });
            });

        return services;
    }

    public static IServiceCollection AddHolmesWebStack(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new UlidIdJsonConverter());
            });

        return services;
    }

    public static IServiceCollection AddHolmesApplicationCore(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AssignUserBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<ICurrentUserInitializer>();
            config.RegisterServicesFromAssemblyContaining<RegisterExternalUserCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<RegisterCustomerCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<RegisterSubjectCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateOrderCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<IssueIntakeInviteCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateNotificationCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<StartSlaClockCommandHandler>();
            config.RegisterServicesFromAssemblyContaining<CreateServiceCommandHandler>();
            // Integration handlers (cross-module event handlers)
            config.RegisterServicesFromAssemblyContaining<IntakeToWorkflowHandler>();
        });

        return services;
    }

    public static IServiceCollection AddHolmesDataProtection(
        this IServiceCollection services,
        IWebHostEnvironment environment
    )
    {
        var isRunningInTestHost = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TESTHOST"),
            "1",
            StringComparison.Ordinal);
        var isTestEnvironment = environment.IsEnvironment("Testing");

        var dataProtection = services.AddDataProtection()
            .SetApplicationName("Holmes");

        if (isTestEnvironment || isRunningInTestHost)
        {
            dataProtection.UseEphemeralDataProtectionProvider();
        }
        else
        {
            dataProtection.PersistKeysToDbContext<CoreDbContext>();
        }

        return services;
    }

    public static IServiceCollection AddHolmesSwagger(
        this IServiceCollection services,
        IWebHostEnvironment environment
    )
    {
        if (!environment.IsDevelopment())
        {
            return services;
        }

        services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Holmes API"
                });
            });

        return services;
    }

    public static IServiceCollection AddHolmesHostedServices(
        this IServiceCollection services,
        IWebHostEnvironment environment
    )
    {
        if (environment.IsDevelopment())
        {
            services.AddHostedService<SeedData>();
        }

        services.AddHostedService<DeferredDispatchProcessor>();
        services.AddHostedService<NotificationProcessingService>();
        services.AddHostedService<SlaClockWatchdogService>();

        return services;
    }

    public static IServiceCollection AddHolmesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var isRunningInTestHost = string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TESTHOST"),
            "1",
            StringComparison.Ordinal);
        var isTestEnvironment = environment.IsEnvironment("Testing");
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString) || isTestEnvironment || isRunningInTestHost)
        {
            return services.AddInfrastructureForTesting();
        }

        return services.AddInfrastructureForProduction(connectionString);
    }

    private static IServiceCollection AddInfrastructureForProduction(
        this IServiceCollection services,
        string connectionString
    )
    {
        ServerVersion serverVersion;
        try
        {
            serverVersion = ServerVersion.AutoDetect(connectionString);
        }
        catch (MySqlException)
        {
            serverVersion = new MySqlServerVersion(new Version(8, 0, 34));
        }

        services.AddCoreInfrastructureSql(connectionString, serverVersion);
        services.AddCoreInfrastructureSecurity();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        services.AddUsersInfrastructureSql(connectionString, serverVersion);
        services.AddCustomersInfrastructureSql(connectionString, serverVersion);
        services.AddSubjectsInfrastructureSql(connectionString, serverVersion);
        services.AddIntakeSessionsInfrastructureSql(connectionString, serverVersion);
        services.AddSingleton<IIntakeSectionMappingService, IntakeSectionMappingService>();
        services.AddOrdersInfrastructureSql(connectionString, serverVersion);
        services.AddNotificationsInfrastructureSql(connectionString, serverVersion);
        services.AddSlaClockInfrastructureSql(connectionString, serverVersion);
        services.AddServicesInfrastructureSql(connectionString, serverVersion);

        services.AddAppApplication();

        return services;
    }

    private static IServiceCollection AddInfrastructureForTesting(
        this IServiceCollection services
    )
    {
        services.AddDbContext<CoreDbContext>(options => options.UseInMemoryDatabase("holmes-core"));
        services.AddDbContext<UsersDbContext>(options => options.UseInMemoryDatabase("holmes-users"));
        services.AddDbContext<CustomersDbContext>(options => options.UseInMemoryDatabase("holmes-customers"));
        services.AddDbContext<SubjectsDbContext>(options => options.UseInMemoryDatabase("holmes-subjects"));
        services.AddDbContext<IntakeSessionsDbContext>(options => options.UseInMemoryDatabase("holmes-intake"));
        services.AddDbContext<OrdersDbContext>(options => options.UseInMemoryDatabase("holmes-workflow"));
        services.AddDbContext<SlaClocksDbContext>(options => options.UseInMemoryDatabase("holmes-slaclocks"));
        services.AddDbContext<ServicesDbContext>(options => options.UseInMemoryDatabase("holmes-services"));
        services.AddSingleton<IAeadEncryptor, NoOpAeadEncryptor>();

        // Event store infrastructure (for testing)
        services.AddScoped<IEventStore, SqlEventStore>();
        services.AddSingleton<IDomainEventSerializer, DomainEventSerializer>();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IUserAccessQueries, UserAccessQueries>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IUserProjectionWriter, UserProjectionWriter>();
        services.AddScoped<ICustomersUnitOfWork, CustomersUnitOfWork>();
        services.AddScoped<ICustomerAccessQueries, CustomerAccessQueries>();
        services.AddScoped<ICustomerQueries, CustomerQueries>();
        services.AddScoped<ICustomerProjectionWriter, CustomerProjectionWriter>();
        services.AddScoped<ISubjectsUnitOfWork, SubjectsUnitOfWork>();
        services.AddScoped<ISubjectQueries, SubjectQueries>();
        services.AddScoped<ISubjectProjectionWriter, SubjectProjectionWriter>();
        services.AddScoped<IIntakeSessionsUnitOfWork, IntakeSessionsUnitOfWork>();
        services.AddScoped<IIntakeSessionProjectionWriter, IntakeSessionProjectionWriter>();
        services.AddScoped<IConsentArtifactStore, DatabaseConsentArtifactStore>();
        services.AddSingleton<IIntakeSectionMappingService, IntakeSectionMappingService>();
        services.AddScoped<IOrdersUnitOfWork, OrdersUnitOfWork>();
        services.AddScoped<IOrderTimelineWriter, OrderTimelineWriter>();
        services.AddScoped<IOrderSummaryWriter, OrderSummaryWriter>();
        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddSingleton<IOrderChangeBroadcaster, OrderChangeBroadcaster>();
        services.AddSingleton<IServiceChangeBroadcaster, ServiceChangeBroadcaster>();
        services.AddScoped<IServicesUnitOfWork, ServicesUnitOfWork>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServiceCatalogRepository, ServiceCatalogRepository>();
        services.AddScoped<IServiceQueries, ServiceQueries>();
        services.AddScoped<IServiceCatalogQueries, ServiceCatalogQueries>();
        services.AddScoped<ICustomerProfileRepository, CustomerProfileRepository>();
        services.AddDbContext<NotificationsDbContext>(options => options.UseInMemoryDatabase("holmes-notifications"));
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationQueries, NotificationQueries>();
        services.AddScoped<INotificationProjectionWriter, NotificationProjectionWriter>();
        services.AddScoped<INotificationProvider, LoggingEmailProvider>();
        services.AddScoped<INotificationProvider, LoggingSmsProvider>();
        services.AddScoped<INotificationProvider, LoggingWebhookProvider>();
        services.AddScoped<ISlaClocksUnitOfWork, SlaClocksUnitOfWork>();
        services.AddScoped<ISlaClockRepository, SlaClockRepository>();
        services.AddScoped<ISlaClockQueries, SlaClockQueries>();
        services.AddScoped<ISlaClockProjectionWriter, SlaClockProjectionWriter>();
        services.AddScoped<IBusinessCalendarService, BusinessCalendarService>();
        services.AddSingleton<ISlaClockChangeBroadcaster, SlaClockChangeBroadcaster>();
        services.AddAppApplication();
        return services;
    }
}
