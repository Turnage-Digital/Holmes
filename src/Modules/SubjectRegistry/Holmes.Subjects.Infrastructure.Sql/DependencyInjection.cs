using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.Subjects.Infrastructure.Sql;

public static class DependencyInjection
{
    public static IServiceCollection AddSubjectsInfrastructureSql(
        this IServiceCollection services,
        string connectionString,
        ServerVersion serverVersion
    )
    {
        services.AddDbContext<SubjectsDbContext>(options =>
            options.UseMySql(connectionString, serverVersion, builder =>
                builder.MigrationsAssembly(typeof(SubjectsDbContext).Assembly.FullName)));

        services.AddScoped<ISubjectRepository, SqlSubjectRepository>();
        services.AddScoped<ISubjectsUnitOfWork, SubjectsUnitOfWork>();

        return services;
    }
}