using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace AutoMarket.Api.HealthChecks;

// Cavabda exception mətni göstərilmir, yalnız ümumi təsvir (SEC-ERR-02)
internal sealed class PostgresHealthCheck(NpgsqlDataSource dataSource) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var command = dataSource.CreateCommand("SELECT 1");
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (NpgsqlException exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "PostgreSQL is unreachable.", exception);
        }
    }
}
