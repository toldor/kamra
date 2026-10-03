using KamraApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KamraApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // QA-7: a missing connection string stops the app at startup instead of failing on the first request.
        services.AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = configuration.GetConnectionString("Default") ?? "")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ADR-0011: Npgsql "Include Error Detail" and EnableSensitiveDataLogging stay off, so data
        // values never reach exception messages or logs.
        services.AddDbContext<KamraDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        return services;
    }
}
