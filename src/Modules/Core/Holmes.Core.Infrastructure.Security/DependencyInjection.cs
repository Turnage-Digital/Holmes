using Holmes.Core.Contracts.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Core.Infrastructure.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreInfrastructureSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IAeadEncryptor, NoOpAeadEncryptor>();

        return services;
    }
}