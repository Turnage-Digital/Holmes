using Holmes.Core.Infrastructure.Sql;
using Holmes.Intake.Infrastructure.Sql;
using Holmes.Intake.Infrastructure.Sql.Projections;
using Holmes.Workflow.Infrastructure.Sql;
using Holmes.Workflow.Infrastructure.Sql.Projections;
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

builder.Services.AddCoreInfrastructureSql(connectionString, serverVersion);
builder.Services.AddIntakeInfrastructureSql(connectionString, serverVersion);
builder.Services.AddWorkflowInfrastructureSql(connectionString, serverVersion);
builder.Services.AddScoped<OrderSummaryProjectionRunner>();
builder.Services.AddScoped<IntakeSessionProjectionRunner>();
builder.Services.AddScoped<OrderTimelineProjectionRunner>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
    .CreateLogger("Holmes.Projections");

var projection = builder.Configuration["projection"] ?? "order-summary";
var reset = builder.Configuration.GetValue("reset", false);
var cancellationToken = CreateCancellationToken();

try
{
    switch (projection.ToLowerInvariant())
    {
        case "order-summary":
            var runner = scope.ServiceProvider.GetRequiredService<OrderSummaryProjectionRunner>();
            var result = await runner.RunAsync(reset, cancellationToken);
            logger.LogInformation(
                "Order summary replay complete. Processed {Count} orders. Last cursor: {OrderId} at {Timestamp}.",
                result.Processed,
                result.LastEntityId ?? "(none)",
                result.LastUpdatedAt?.ToString("O") ?? "(n/a)");
            break;
        case "intake-sessions":
            var intakeRunner = scope.ServiceProvider.GetRequiredService<IntakeSessionProjectionRunner>();
            var intakeResult = await intakeRunner.RunAsync(reset, cancellationToken);
            logger.LogInformation(
                "Intake session replay complete. Processed {Count} sessions. Last cursor: {SessionId} at {Timestamp}.",
                intakeResult.Processed,
                intakeResult.LastEntityId ?? "(none)",
                intakeResult.LastUpdatedAt?.ToString("O") ?? "(n/a)");
            break;
        case "order-timeline":
            var timelineRunner = scope.ServiceProvider.GetRequiredService<OrderTimelineProjectionRunner>();
            var timelineResult = await timelineRunner.RunAsync(reset, cancellationToken);
            logger.LogInformation(
                "Order timeline replay complete. Wrote {Count} events. Last event {OrderId} at {Timestamp}.",
                timelineResult.Processed,
                timelineResult.LastEntityId ?? "(none)",
                timelineResult.LastUpdatedAt?.ToString("O") ?? "(n/a)");
            break;
        default:
            logger.LogError(
                "Unknown projection '{Projection}'. Expected one of: order-summary, intake-sessions, order-timeline.",
                projection);
            Environment.ExitCode = 1;
            return;
    }
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