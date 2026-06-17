using FinOps.Application.Cloud;
using FinOps.Application.Etl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinOps.Infrastructure.Persistence;

public static class PostgreSqlServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSql(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(PostgreSqlHealthCheckOptions.SectionName)
            .Get<PostgreSqlHealthCheckOptions>()
            ?? new PostgreSqlHealthCheckOptions();

        services.AddDbContextFactory<FinOpsDbContext>(dbOptions =>
            dbOptions.UseNpgsql(options.GetConnectionString()));
        services.AddScoped<ICloudResourceRepository, CloudResourceRepository>();
        services.AddScoped<ICloudCostRepository, CloudCostRepository>();
        services.AddScoped<ICloudCostQueryRepository, CloudCostQueryRepository>();
        services.AddScoped<IEtlJobRunRepository, EtlJobRunRepository>();

        return services;
    }
}
