using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Repositories;
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

        services.AddScoped<IUserRepository, SqlUserRepository>();
        services.AddScoped<IUserDirectory, SqlUserDirectory>();
        services.AddScoped<IUsersUnitOfWork, UsersUnitOfWork>();

        return services;
    }
}