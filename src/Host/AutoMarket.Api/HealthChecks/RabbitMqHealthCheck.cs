using AutoMarket.Api.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AutoMarket.Api.HealthChecks;

// Hələlik hər yoxlamada qısa ömürlü bağlantı açılır. Messaging infrastrukturu (ADR-0005) yazılanda
// paylaşılan connection-ın vəziyyəti yoxlanılacaq
internal sealed class RabbitMqHealthCheck(
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptions<DependencyHealthCheckOptions> healthCheckOptions) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitMqOptions.Value.ConnectionString),
            RequestedConnectionTimeout = healthCheckOptions.Value.Timeout,
            AutomaticRecoveryEnabled = false,
            ClientProvidedName = "automarket-health-check",
        };

        try
        {
            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ connection is closed.");
        }
        catch (BrokerUnreachableException exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ is unreachable.", exception);
        }
    }
}
