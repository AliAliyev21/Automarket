namespace AutoMarket.Api.Configuration;

// /health/ready yoxlamalarının vaxt limiti (ARCHITECTURE §8.5). Dəyər appsettings.json-dadır
internal sealed class DependencyHealthCheckOptions
{
    public const string SectionName = "HealthChecks";

    public TimeSpan Timeout { get; set; }
}
