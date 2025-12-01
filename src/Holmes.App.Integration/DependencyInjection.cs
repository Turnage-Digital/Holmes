using Holmes.App.Integration.Gateways;
using Holmes.Intake.Application.Gateways;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Integration;

public static class DependencyInjection
{
    public static IServiceCollection AddAppIntegration(this IServiceCollection services)
    {
        services.AddScoped<IOrderWorkflowGateway, OrderWorkflowGateway>();

        return services;
    }
}
