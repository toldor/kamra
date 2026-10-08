using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace KamraApp.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck(KamraDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database is not reachable.");
}
