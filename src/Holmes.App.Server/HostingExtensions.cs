using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.App.Server.Security;
using Holmes.Core.Application;
using Holmes.Core.Application.Behaviors;
using Holmes.Core.Domain.Security;
using Holmes.Core.Infrastructure.Security;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Repositories;
using Holmes.Subjects.Application.Commands;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql.Repositories;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Holmes.Users.Infrastructure.Sql.Repositories;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using Serilog;
using Serilog.Events;

namespace Holmes.App.Server;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, config) =>
        {
            var seqUrl = ctx.Configuration["Seq:Url"];
            var seqApiKey = ctx.Configuration["Seq:ApiKey"];

            config.WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level} {SourceContext}]{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
                .Enrich.WithCorrelationIdHeader("X-Correlation-ID")
                .Enrich.FromLogContext();

            if (!string.IsNullOrWhiteSpace(seqUrl))
            {
                if (!string.IsNullOrWhiteSpace(seqApiKey))
                {
                    config.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
                }
                else
                {
                    config.WriteTo.Seq(seqUrl);
                }
            }
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddHealthChecks();

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter( /* JsonNamingPolicy.CamelCase */));
            });

        builder.Services.AddAuthentication();
        builder.Services.AddScoped<IUserContext, HttpUserContext>();

        var isRunningInTestHost = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_TESTHOST"), "1",
            StringComparison.Ordinal);
        var isTestEnvironment = builder.Environment.IsEnvironment("Testing");
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || isTestEnvironment || isRunningInTestHost)
        {
            builder.Services.AddInfrastructureForTesting();
        }
        else
        {
            builder.Services.AddInfrastructure(connectionString);
        }

        builder.Services.AddDomain();
        builder.Services.AddApplication();

        builder.Services.AddAuthorization();

        var dataProtection = builder.Services.AddDataProtection()
            .SetApplicationName("Holmes");
        if (isTestEnvironment || isRunningInTestHost)
        {
            dataProtection.UseEphemeralDataProtectionProvider();
        }
        else
        {
            dataProtection.PersistKeysToDbContext<CoreDbContext>();
        }

        if (builder.Environment.IsDevelopment())
        {
            builder.Services
                .AddEndpointsApiExplorer()
                .AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Holmes API"
                    });
                });
        }

        var retval = builder.Build();
        return retval;
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();
        app.Map("/error", (HttpContext context, IHostEnvironment env, ILogger<Program> logger) =>
        {
            var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
            if (exceptionFeature?.Error is not null)
            {
                logger.LogError(exceptionFeature.Error, "Unhandled request error");
            }
            var detail = env.IsDevelopment() || env.IsEnvironment("Testing")
                ? exceptionFeature?.Error.ToString()
                : null;

            return Results.Problem(statusCode: StatusCodes.Status500InternalServerError, detail: detail);
        }).AllowAnonymous();
        app.MapGet("/_info", (IHostEnvironment env) =>
                Results.Ok(new
                {
                    service = typeof(HostingExtensions).Assembly.GetName().Name,
                    version = typeof(HostingExtensions).Assembly.GetName().Version?.ToString(),
                    environment = env.EnvironmentName,
                    machine = Environment.MachineName
                }))
            .AllowAnonymous();

        return app;
    }

    private static IServiceCollection AddInfrastructure(
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

        /* Core */
        services.AddDbContextWithMigrations<CoreDbContext>(connectionString, serverVersion);
        services.AddSingleton<IAeadEncryptor, NoOpAeadEncryptor>();

        /* Users */
        services.AddUsersInfrastructureSql(connectionString, serverVersion);

        /* Customers */
        services.AddCustomersInfrastructureSql(connectionString, serverVersion);

        /* Subjects */
        services.AddSubjectsInfrastructureSql(connectionString, serverVersion);

        return services;
    }

    private static IServiceCollection AddInfrastructureForTesting(this IServiceCollection services)
    {
        services.AddDbContext<CoreDbContext>(options => options.UseInMemoryDatabase("holmes-core"));
        services.AddDbContext<UsersDbContext>(options => options.UseInMemoryDatabase("holmes-users"));
        services.AddDbContext<CustomersDbContext>(options => options.UseInMemoryDatabase("holmes-customers"));
        services.AddDbContext<SubjectsDbContext>(options => options.UseInMemoryDatabase("holmes-subjects"));
        services.AddSingleton<IAeadEncryptor, NoOpAeadEncryptor>();
        services.AddScoped<IUserRepository, SqlUserRepository>();
        services.AddScoped<IUserDirectory, SqlUserDirectory>();
        services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();
        services.AddScoped<ICustomerRepository, SqlCustomerRepository>();
        services.AddScoped<ICustomersUnitOfWork, CustomersUnitOfWork>();
        services.AddScoped<ISubjectRepository, SqlSubjectRepository>();
        services.AddScoped<ISubjectsUnitOfWork, SubjectsUnitOfWork>();
        return services;
    }

    private static IServiceCollection AddDomain(this IServiceCollection services)
    {
        // services.AddScoped<IValidateListItemBag<ListDb>, ListItemBagValidator<ListDb, ItemDb>>();
        // services.AddScoped<ListsAggregate<ListDb, ItemDb>>();
        // services.AddScoped<INotificationTriggerEvaluator, NotificationTriggerEvaluator>();
        // services.AddScoped<NotificationAggregate<NotificationRuleDb, NotificationDb>>();
        return services;
    }

    private static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblyContaining<RequestBase>();
            config.RegisterServicesFromAssemblyContaining<RegisterExternalUserCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterCustomerCommand>();
            config.RegisterServicesFromAssemblyContaining<RegisterSubjectCommand>();
        });

        // services.AddTransient(typeof(IPipelineBehavior<,>),
        //     typeof(AssignUserBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(LoggingBehavior<,>));

        // services.AddScoped<IMigrationValidator, MigrationValidator>();
        // services.AddScoped<ListMigrationJobRunner>();

        // // Lists - close generic handlers in composition root
        // services.AddScoped(typeof(IRequestHandler<ConvertTextToListItemCommand, ListItemDto>),
        //     typeof(ConvertTextToListItemCommandHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<CreateListCommand, ListItemDefinitionDto>),
        //     typeof(CreateListCommandHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<CreateListItemCommand, ListItemDto>),
        //     typeof(CreateListItemCommandHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<DeleteListCommand>),
        //     typeof(DeleteListCommandHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<DeleteListItemCommand>),
        //     typeof(DeleteListItemCommandHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<UpdateListItemCommand>),
        //     typeof(UpdateListItemCommandHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<RunMigrationCommand, MigrationResult>),
        //     typeof(RunMigrationCommandHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<GetStatusTransitionsQuery, StatusTransition[]>),
        //     typeof(GetStatusTransitionsQueryHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<UpdateListCommand>),
        //     typeof(UpdateListCommandHandler<ListDb, ItemDb>));
        // services.AddScoped(typeof(IRequestHandler<UpdateListItemCommand>),
        //     typeof(UpdateListItemCommandHandler<ListDb, ItemDb>));
        //
        // // Notifications - close generic handlers in composition root
        // services.AddScoped(typeof(IRequestHandler<CreateNotificationRuleCommand, NotificationRuleDto>),
        //     typeof(CreateNotificationRuleCommandHandler<NotificationRuleDb, NotificationDb>));
        // services.AddScoped(typeof(IRequestHandler<UpdateNotificationRuleCommand>),
        //     typeof(UpdateNotificationRuleCommandHandler<NotificationRuleDb, NotificationDb>));
        // services.AddScoped(typeof(IRequestHandler<DeleteNotificationRuleCommand>),
        //     typeof(DeleteNotificationRuleCommandHandler<NotificationRuleDb, NotificationDb>));
        // services.AddScoped(typeof(IRequestHandler<MarkNotificationAsReadCommand>),
        //     typeof(MarkNotificationAsReadCommandHandler<NotificationRuleDb, NotificationDb>));
        // services.AddScoped(typeof(IRequestHandler<MarkAllNotificationsAsReadCommand>),
        //     typeof(MarkAllNotificationsAsReadCommandHandler<NotificationRuleDb, NotificationDb>));
        // services.AddScoped(typeof(IRequestHandler<GetNotificationDetailsQuery, NotificationDetailsDto?>),
        //     typeof(GetNotificationDetailsQueryHandler<NotificationRuleDb, NotificationDb>));
        // services.AddScoped(typeof(IRequestHandler<GetUserNotificationsQuery, NotificationListPageDto>),
        //     typeof(GetUserNotificationsQueryHandler<NotificationRuleDb, NotificationDb>));
        // services.AddScoped<IRequestHandler<GetUserNotificationRulesQuery, NotificationRuleDto[]>,
        //     GetUserNotificationRulesQueryHandler>();
        // services.AddScoped(typeof(IRequestHandler<GetUnreadNotificationCountQuery, int>),
        //     typeof(GetUnreadNotificationCountQueryHandler<NotificationRuleDb, NotificationDb>));
        //
        // // Background workers
        // services.AddHostedService<NotificationDeliveryService>();
        // services.AddHostedService<OutboxDispatcherService>();
        // services.AddHostedService<ListMigrationDispatcherService>();
        return services;
    }

    private static IServiceCollection AddDbContextWithMigrations<TContext>(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    ) where TContext : DbContext
    {
        var migrationsAssembly = typeof(TContext).Assembly.FullName!;
        services.AddDbContext<TContext>(options => options.UseMySql(connectionString, serverVersion,
            optionsBuilder => optionsBuilder.MigrationsAssembly(migrationsAssembly)));
        return services;
    }
}
