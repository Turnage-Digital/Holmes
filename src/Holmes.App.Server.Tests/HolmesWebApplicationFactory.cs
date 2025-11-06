using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

internal class HolmesWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(WebHostDefaults.DetailedErrorsKey, "true");
        builder.CaptureStartupErrors(true);
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(TestAuthDefaults.Scheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthDefaults.Scheme, _ => { });
        });
    }
}
