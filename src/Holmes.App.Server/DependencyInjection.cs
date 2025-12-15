using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.App.Integration;
using Holmes.App.Server.Infrastructure;
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
using Holmes.Customers.Application.Abstractions.Projections;
using Holmes.Customers.Application.Abstractions.Queries;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Projections;
using Holmes.Customers.Infrastructure.Sql.Queries;
using Holmes.Customers.Infrastructure.Sql.Repositories;
using Holmes.Intake.Application.Abstractions.Projections;
using Holmes.Intake.Application.Commands;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql;
using Holmes.Intake.Infrastructure.Sql.Projections;
using Holmes.Notifications.Application.Abstractions.Projections;
using Holmes.Notifications.Application.Abstractions.Queries;
using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql;
using Holmes.Notifications.Infrastructure.Sql.Projections;
using Holmes.Notifications.Infrastructure.Sql.Queries;
using Holmes.Services.Application.Abstractions;
using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Application.Commands;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql;
using Holmes.Services.Infrastructure.Sql.Queries;
using Holmes.SlaClocks.Application.Abstractions.Projections;
using Holmes.SlaClocks.Application.Abstractions.Queries;
using Holmes.SlaClocks.Application.Abstractions.Services;
using Holmes.SlaClocks.Application.Commands;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql;
using Holmes.SlaClocks.Infrastructure.Sql.Projections;
using Holmes.SlaClocks.Infrastructure.Sql.Queries;
using Holmes.SlaClocks.Infrastructure.Sql.Services;
using Holmes.Subjects.Application.Abstractions.Projections;
using Holmes.Subjects.Application.Abstractions.Queries;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Projections;
using Holmes.Subjects.Infrastructure.Sql.Queries;
using Holmes.Users.Application.Abstractions;
using Holmes.Users.Application.Abstractions.Projections;
using Holmes.Users.Application.Abstractions.Queries;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Holmes.Users.Infrastructure.Sql.Projections;
using Holmes.Users.Infrastructure.Sql.Queries;
using Holmes.Users.Infrastructure.Sql.Repositories;
using Holmes.Workflow.Application.Abstractions.Notifications;
using Holmes.Workflow.Application.Abstractions.Projections;
using Holmes.Workflow.Application.Abstractions.Queries;
using Holmes.Workflow.Application.Commands;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql;
using Holmes.Workflow.Infrastructure.Sql.Notifications;
using Holmes.Workflow.Infrastructure.Sql.Projections;
using Holmes.Workflow.Infrastructure.Sql.Queries;
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
            config.RegisterServicesFromAssemblyContaining<RequestBase>();
            config.RegisterServicesFromAssemblyContaining<RegisterExternalUserCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterCustomerCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterSubjectCommand>();
            config.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
            config.RegisterServicesFromAssemblyContaining<IssueIntakeInviteCommand>();
            config.RegisterServicesFromAssemblyContaining<CreateNotificationRequestCommand>();
            config.RegisterServicesFromAssemblyContaining<StartSlaClockCommand>();
            config.RegisterServicesFromAssemblyContaining<CreateServiceRequestCommand>();
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
        services.AddIntakeInfrastructureSql(connectionString, serverVersion);
        services.AddWorkflowInfrastructureSql(connectionString, serverVersion);
        services.AddNotificationsInfrastructureSql(connectionString, serverVersion);
        services.AddSlaClockInfrastructureSql(connectionString, serverVersion);
        services.AddServicesInfrastructureSql(connectionString, serverVersion);

        services.AddAppIntegration();
        services.AddSingleton<IOrderChangeBroadcaster, OrderChangeBroadcaster>();

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
        services.AddDbContext<IntakeDbContext>(options => options.UseInMemoryDatabase("holmes-intake"));
        services.AddDbContext<WorkflowDbContext>(options => options.UseInMemoryDatabase("holmes-workflow"));
        services.AddDbContext<SlaClockDbContext>(options => options.UseInMemoryDatabase("holmes-slaclocks"));
        services.AddDbContext<ServicesDbContext>(options => options.UseInMemoryDatabase("holmes-services"));
        services.AddSingleton<IAeadEncryptor, NoOpAeadEncryptor>();

        // Event store infrastructure (for testing)
        services.AddScoped<IEventStore, SqlEventStore>();
        services.AddSingleton<IDomainEventSerializer, DomainEventSerializer>();
        services.AddScoped<ITenantContext, HttpTenantContext>();

        services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();
        services.AddScoped<IUserDirectory, SqlUserDirectory>();
        services.AddScoped<IUserAccessQueries, SqlUserAccessQueries>();
        services.AddScoped<IUserQueries, SqlUserQueries>();
        services.AddScoped<IUserProjectionWriter, SqlUserProjectionWriter>();
        services.AddScoped<ICustomersUnitOfWork, CustomersUnitOfWork>();
        services.AddScoped<ICustomerAccessQueries, SqlCustomerAccessQueries>();
        services.AddScoped<ICustomerQueries, SqlCustomerQueries>();
        services.AddScoped<ICustomerProjectionWriter, SqlCustomerProjectionWriter>();
        services.AddScoped<ISubjectsUnitOfWork, SubjectsUnitOfWork>();
        services.AddScoped<ISubjectQueries, SqlSubjectQueries>();
        services.AddScoped<ISubjectProjectionWriter, SqlSubjectProjectionWriter>();
        services.AddScoped<IIntakeUnitOfWork, IntakeUnitOfWork>();
        services.AddScoped<IIntakeSessionProjectionWriter, SqlIntakeSessionProjectionWriter>();
        services.AddScoped<IConsentArtifactStore, DatabaseConsentArtifactStore>();
        services.AddScoped<IWorkflowUnitOfWork, WorkflowUnitOfWork>();
        services.AddScoped<IOrderTimelineWriter, SqlOrderTimelineWriter>();
        services.AddScoped<IOrderSummaryWriter, SqlOrderSummaryWriter>();
        services.AddScoped<IOrderQueries, SqlOrderQueries>();
        services.AddSingleton<IOrderChangeBroadcaster, OrderChangeBroadcaster>();
        services.AddSingleton<IServiceChangeBroadcaster, ServiceChangeBroadcaster>();
        services.AddScoped<IServicesUnitOfWork, ServicesUnitOfWork>();
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<IServiceCatalogRepository, ServiceCatalogRepository>();
        services.AddScoped<IServiceRequestQueries, SqlServiceRequestQueries>();
        services.AddScoped<IServiceCatalogQueries, SqlServiceCatalogQueries>();
        services.AddScoped<ICustomerProfileRepository, SqlCustomerProfileRepository>();
        services.AddDbContext<NotificationsDbContext>(options => options.UseInMemoryDatabase("holmes-notifications"));
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();
        services.AddScoped<INotificationRequestRepository, NotificationRequestRepository>();
        services.AddScoped<INotificationQueries, SqlNotificationQueries>();
        services.AddScoped<INotificationProjectionWriter, SqlNotificationProjectionWriter>();
        services.AddScoped<INotificationProvider, LoggingEmailProvider>();
        services.AddScoped<INotificationProvider, LoggingSmsProvider>();
        services.AddScoped<INotificationProvider, LoggingWebhookProvider>();
        services.AddScoped<ISlaClockUnitOfWork, SlaClockUnitOfWork>();
        services.AddScoped<ISlaClockRepository, SlaClockRepository>();
        services.AddScoped<ISlaClockQueries, SqlSlaClockQueries>();
        services.AddScoped<ISlaClockProjectionWriter, SqlSlaClockProjectionWriter>();
        services.AddScoped<IBusinessCalendarService, BusinessCalendarService>();
        services.AddAppIntegration();
        return services;
    }
}