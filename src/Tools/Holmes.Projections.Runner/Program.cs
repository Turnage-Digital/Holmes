using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using Holmes.Customers.Application.EventHandlers;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.IntakeSessions.Application.EventHandlers;
using Holmes.IntakeSessions.Infrastructure.Sql;
using Holmes.Orders.Application.EventHandlers;
using Holmes.Orders.Infrastructure.Sql;
using Holmes.Subjects.Application.EventHandlers;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Users.Application.EventHandlers;
using Holmes.Users.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", true, false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "HH:mm:ss ";
    options.SingleLine = true;
    options.IncludeScopes = false;
});

var connectionString = builder.Configuration.GetConnectionString("Holmes");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Holmes must be configured.");
}

ServerVersion serverVersion;
try
{
    serverVersion = ServerVersion.AutoDetect(connectionString);
}
catch (MySqlException)
{
    serverVersion = new MySqlServerVersion(new Version(8, 0, 34));
}

// Register all infrastructure modules
builder.Services.AddCoreInfrastructureSql(connectionString, serverVersion);
builder.Services.AddCustomersInfrastructureSql(connectionString, serverVersion);
builder.Services.AddIntakeSessionsInfrastructureSql(connectionString, serverVersion);
builder.Services.AddOrdersInfrastructureSql(connectionString, serverVersion);
builder.Services.AddSubjectsInfrastructureSql(connectionString, serverVersion);
builder.Services.AddUsersInfrastructureSql(connectionString, serverVersion);

// Register MediatR with all event handlers for event-based replay
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssemblyContaining<UserProjectionHandler>();
    config.RegisterServicesFromAssemblyContaining<CustomerProjectionHandler>();
    config.RegisterServicesFromAssemblyContaining<SubjectProjectionHandler>();
    config.RegisterServicesFromAssemblyContaining<IntakeSessionProjectionHandler>();
    config.RegisterServicesFromAssemblyContaining<OrderStatusChangedHandler>();
});

// Register aggregate-based projection runners (existing)
builder.Services.AddScoped<OrderSummaryProjectionRunner>();
builder.Services.AddScoped<IntakeSessionProjectionRunner>();
builder.Services.AddScoped<OrderTimelineProjectionRunner>();

// Register event-based projection runners (new)
builder.Services.AddScoped<UserEventProjectionRunner>();
builder.Services.AddScoped<CustomerEventProjectionRunner>();
builder.Services.AddScoped<SubjectEventProjectionRunner>();
builder.Services.AddScoped<IntakeSessionEventProjectionRunner>();
builder.Services.AddScoped<OrderSummaryEventProjectionRunner>();
builder.Services.AddScoped<OrderTimelineEventProjectionRunner>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
    .CreateLogger("Holmes.Projections");

var projection = builder.Configuration["projection"] ?? "order-summary";
var reset = builder.Configuration.GetValue("reset", false);
var fromEvents = builder.Configuration.GetValue("from-events", false);
var batchSize = builder.Configuration.GetValue("batch-size", 200);
var cancellationToken = CreateCancellationToken();

try
{
    ProjectionReplayResult result;

    if (fromEvents)
    {
        result = await RunEventBasedProjectionAsync(projection, reset, batchSize, scope.ServiceProvider,
            cancellationToken);
    }
    else
    {
        result = await RunAggregateBasedProjectionAsync(projection, reset, scope.ServiceProvider, cancellationToken);
    }

    logger.LogInformation(
        "Projection replay complete. Processed {Count} items. Last: {EntityId} at {Timestamp}.",
        result.Processed,
        result.LastEntityId ?? "(none)",
        result.LastUpdatedAt?.ToString("O") ?? "(n/a)");
}
catch (OperationCanceledException)
{
    logger.LogWarning("Projection replay canceled.");
    Environment.ExitCode = 2;
}
catch (Exception ex)
{
    logger.LogError(ex, "Projection replay failed.");
    Environment.ExitCode = 1;
}

return;

static async Task<ProjectionReplayResult> RunEventBasedProjectionAsync(
    string projection,
    bool reset,
    int batchSize,
    IServiceProvider services,
    CancellationToken cancellationToken
)
{
    return projection.ToLowerInvariant() switch
    {
        "user" or "users" =>
            await services.GetRequiredService<UserEventProjectionRunner>()
                .RunAsync(reset, batchSize, cancellationToken),

        "customer" or "customers" =>
            await services.GetRequiredService<CustomerEventProjectionRunner>()
                .RunAsync(reset, batchSize, cancellationToken),

        "subject" or "subjects" =>
            await services.GetRequiredService<SubjectEventProjectionRunner>()
                .RunAsync(reset, batchSize, cancellationToken),

        "intake-sessions" =>
            await services.GetRequiredService<IntakeSessionEventProjectionRunner>()
                .RunAsync(reset, batchSize, cancellationToken),

        "order-summary" =>
            await services.GetRequiredService<OrderSummaryEventProjectionRunner>()
                .RunAsync(reset, batchSize, cancellationToken),

        "order-timeline" =>
            await services.GetRequiredService<OrderTimelineEventProjectionRunner>()
                .RunAsync(reset, batchSize, cancellationToken),

        _ => throw new InvalidOperationException(
            $"Unknown projection '{projection}' for event-based replay. " +
            "Expected one of: user, customer, subject, intake-sessions, order-summary, order-timeline.")
    };
}

static async Task<ProjectionReplayResult> RunAggregateBasedProjectionAsync(
    string projection,
    bool reset,
    IServiceProvider services,
    CancellationToken cancellationToken
)
{
    return projection.ToLowerInvariant() switch
    {
        "order-summary" =>
            await services.GetRequiredService<OrderSummaryProjectionRunner>()
                .RunAsync(reset, cancellationToken),

        "intake-sessions" =>
            await services.GetRequiredService<IntakeSessionProjectionRunner>()
                .RunAsync(reset, cancellationToken),

        "order-timeline" =>
            await services.GetRequiredService<OrderTimelineProjectionRunner>()
                .RunAsync(reset, cancellationToken),

        _ => throw new InvalidOperationException(
            $"Unknown projection '{projection}' for aggregate-based replay. " +
            "Expected one of: order-summary, intake-sessions, order-timeline. " +
            "For user, customer, subject projections, use --from-events true.")
    };
}

static CancellationToken CreateCancellationToken()
{
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, args) =>
    {
        args.Cancel = true;
        cts.Cancel();
    };
    return cts.Token;
}