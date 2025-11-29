using System.Security.Claims;
using System.Text;
using Duende.IdentityServer.Services;
using Holmes.Identity.Server;
using Holmes.Identity.Server.Data;
using Holmes.Identity.Server.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level} {SourceContext}]{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .CreateBootstrapLogger();

Log.Information("Starting up");

try
{
    var builder = WebApplication.CreateBuilder(args);

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

    builder.Services.AddOptions<ProvisioningOptions>()
        .Bind(builder.Configuration.GetSection(ProvisioningOptions.SectionName))
        .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey),
            "Provisioning:ApiKey is required")
        .ValidateOnStart();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection must be configured for the Identity host.");
    }

    ServerVersion serverVersion;
    try
    {
        serverVersion = ServerVersion.AutoDetect(connectionString);
    }
    catch
    {
        serverVersion = new MySqlServerVersion(new Version(8, 0, 34));
    }

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString, serverVersion,
            mySqlOptions => mySqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName)));

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders()
        .AddDefaultUI();

    builder.Services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.UserInteraction.LoginUrl = "/Identity/Account/Login";
            options.UserInteraction.LogoutUrl = "/Identity/Account/Logout";
        })
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = db =>
                db.UseMySql(connectionString, serverVersion,
                    mySqlOptions => mySqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName));
        })
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = db =>
                db.UseMySql(connectionString, serverVersion,
                    mySqlOptions => mySqlOptions.MigrationsAssembly(typeof(Program).Assembly.FullName));
            options.EnableTokenCleanup = true;
            options.TokenCleanupInterval = 3600;
        })
        .AddAspNetIdentity<ApplicationUser>()
        .AddProfileService<ProfileService>()
        .AddDeveloperSigningCredential();

    builder.Services.AddDataProtection()
        .PersistKeysToDbContext<ApplicationDbContext>();

    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization();
    builder.Services.AddRazorPages();

    var app = builder.Build();

    await SeedData.EnsureSeedDataAsync(app.Services);

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseIdentityServer();
    app.UseAuthorization();

    app.MapGet("/", () => Results.Redirect("/.well-known/openid-configuration"))
        .AllowAnonymous();
    app.MapRazorPages();

    var provisioningOptions = app.Services
        .GetRequiredService<IOptions<ProvisioningOptions>>()
        .Value;

    app.MapPost("/provision/users", async (
            ProvisionIdentityUserRequest request,
            UserManager<ApplicationUser> userManager,
            IIdentityServerInteractionService interaction,
            HttpContext context
        ) =>
        {
            var apiKey = provisioningOptions.ApiKey ?? string.Empty;
            var headerValue = context.Request.Headers.Authorization.ToString();
            var suppliedToken = headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? headerValue["Bearer ".Length..]
                : headerValue;
            if (!string.Equals(suppliedToken, apiKey, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.HolmesUserId))
            {
                return Results.BadRequest("Email and HolmesUserId are required.");
            }

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    DisplayName = request.DisplayName,
                    EmailConfirmed = false
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return Results.BadRequest(createResult.Errors);
                }
            }

            var existingClaims = await userManager.GetClaimsAsync(user);
            if (existingClaims.All(c => c.Type != "holmes_user_id"))
            {
                await userManager.AddClaimAsync(user, new Claim("holmes_user_id", request.HolmesUserId));
            }

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var baseUrl = provisioningOptions.BaseUrl ?? builder.Configuration["BaseUrl"] ?? string.Empty;
            var returnUrl = string.IsNullOrWhiteSpace(request.ConfirmationReturnUrl)
                ? "/"
                : request.ConfirmationReturnUrl;
            var confirmationLink =
                $"{baseUrl}/Identity/Account/ConfirmEmail?userId={Uri.EscapeDataString(user.Id)}&code={encodedToken}&returnUrl={Uri.EscapeDataString(returnUrl)}";

            return Results.Ok(new
            {
                identityUserId = user.Id,
                email = user.Email,
                confirmationLink
            });
        })
        .AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name is not "HostAbortedException")
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}