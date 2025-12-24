using Holmes.App.Integration.Gateways;
using Holmes.IntakeSessions.Application.Gateways;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Integration;

public static class DependencyInjection
{
    public static IServiceCollection AddAppIntegration(this IServiceCollection services)
    {
        services.AddScoped<IOrderWorkflowGateway, OrderWorkflowGateway>();
        services.AddScoped<ISubjectDataGateway, SubjectDataGateway>();

        return services;
    }
}