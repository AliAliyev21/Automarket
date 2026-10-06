using AutoMarket.Api.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace AutoMarket.Api.HealthChecks;

// NFR-HC, ARCHITECTURE §8.5. Public cavab yalnız status mətnidir; detallar yalnız Development-də
internal static class HealthCheckEndpoints
{
    private const string ReadyTag = "ready";

    public static IServiceCollection AddDependencyHealthChecks(this IServiceCollection services)
    {
        // Redis və RabbitMQ düşəndə sorğular işləməyə davam edir (cache fallback, outbox), ona görə Degraded
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", HealthStatus.Unhealthy, [ReadyTag])
            .AddCheck<RedisHealthCheck>("redis", HealthStatus.Degraded, [ReadyTag])
            .AddCheck<RabbitMqHealthCheck>("rabbitmq", HealthStatus.Degraded, [ReadyTag]);

        // Vaxt limiti konfiqurasiyadan oxunur, registration zamanı options hələ əlçatan deyil
        services.AddOptions<HealthCheckServiceOptions>()
            .PostConfigure<IOptions<DependencyHealthCheckOptions>>((options, dependencyOptions) =>
            {
                foreach (var registration in options.Registrations.Where(r => r.Tags.Contains(ReadyTag)))
                {
                    registration.Timeout = dependencyOptions.Value.Timeout;
                }
            });

        return services;
    }

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints, IHostEnvironment environment)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
            .AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = IsReadinessCheck })
            .AllowAnonymous();

        // ARCHITECTURE §8.5: details daxili şəbəkə və ya Admin üçündür. Admin policy olana qədər yalnız Development
        if (environment.IsDevelopment())
        {
            endpoints.MapHealthChecks("/health/ready/details", new HealthCheckOptions
            {
                Predicate = IsReadinessCheck,
                ResponseWriter = WriteDetailsAsync,
            })
            .AllowAnonymous();
        }

        return endpoints;
    }

    private static bool IsReadinessCheck(HealthCheckRegistration registration) => registration.Tags.Contains(ReadyTag);

    // Exception mətni cavaba yazılmır (SEC-ERR-02), yalnız status, müddət və təsvir
    private static Task WriteDetailsAsync(HttpContext context, HealthReport report)
    {
        var response = new HealthDetailsResponse(
            report.Status.ToString(),
            report.TotalDuration.TotalMilliseconds,
            report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthEntryResponse(entry.Value.Status.ToString(), entry.Value.Duration.TotalMilliseconds, entry.Value.Description),
                StringComparer.Ordinal));

        return context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }

    private sealed record HealthDetailsResponse(string Status, double TotalDurationMs, IReadOnlyDictionary<string, HealthEntryResponse> Checks);

    private sealed record HealthEntryResponse(string Status, double DurationMs, string? Description);
}
