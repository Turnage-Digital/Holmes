using Holmes.Users.Domain.Users;
using Holmes.Users.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

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

        return services;
    }
}
