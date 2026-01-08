using Holmes.Subjects.Contracts;
using Holmes.Subjects.Domain;
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
        services.AddScoped<ISubjectQueries, SubjectQueries>();
        services.AddScoped<ISubjectProjectionWriter, SubjectProjectionWriter>();

        return services;
    }
}
