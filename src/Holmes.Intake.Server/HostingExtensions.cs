using Holmes.Hosting;
using Serilog;

namespace Holmes.Intake.Server;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseHolmesSerilog();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
            app.UseHsts();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();

        app.MapFallbackToFile("index.html");

        return app;
    }
}