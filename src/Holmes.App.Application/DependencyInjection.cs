using Holmes.App.Application.Gateways;
using Holmes.IntakeSessions.Application.Gateways;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAppApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderWorkflowGateway, OrderWorkflowGateway>();
        services.AddScoped<ISubjectDataGateway, SubjectDataGateway>();

        return services;
    }
}