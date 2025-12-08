using Holmes.Subjects.Application.Abstractions.Queries;
using Holmes.Subjects.Domain;
using Holmes.Subjects.Infrastructure.Sql.Queries;
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

        services.AddScoped<ISubjectsUnitOfWork, SubjectsUnitOfWork>();
        services.AddScoped<ISubjectQueries, SqlSubjectQueries>();

        return services;
    }
}