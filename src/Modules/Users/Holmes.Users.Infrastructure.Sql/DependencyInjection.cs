using Holmes.Users.Contracts;
using Holmes.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Users.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<UsersDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(UsersDbContext).Assembly.FullName)));

        services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IUserAccessQueries, UserAccessQueries>();
        services.AddScoped<IUserQueries, UserQueries>();
        services.AddScoped<IUserProjectionWriter, UserProjectionWriter>();

        return services;
    }
}