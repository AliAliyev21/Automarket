using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests;

// NFR-HC, ARCHITECTURE §8.5
public sealed class HealthTests(ContainersFixture containers)
{
    [Fact]
    public async Task Live_Always_ReturnsHealthy()
    {
        // Asılılıqlar əlçatmaz olsa da live yalnız prosesi yoxlayır
        await using var factory = containers.CreateFactory(new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = UnreachableEndpoints.Postgres,
        });

        var (status, body) = await GetAsync(factory, "/health/live");

        status.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe("Healthy");
    }

    [Fact]
    public async Task Ready_AllDependenciesUp_ReturnsHealthy()
    {
        await using var factory = containers.CreateFactory();

        var (status, body) = await GetAsync(factory, "/health/ready");

        status.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe("Healthy");
    }

    [Fact]
    public async Task Ready_PostgresDown_Returns503Unhealthy()
    {
        await using var factory = containers.CreateFactory(new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = UnreachableEndpoints.Postgres,
        });

        var (status, body) = await GetAsync(factory, "/health/ready");

        status.ShouldBe(HttpStatusCode.ServiceUnavailable);
        body.ShouldBe("Unhealthy");
    }

    [Theory]
    [InlineData("Redis:ConnectionString", UnreachableEndpoints.Redis)]
    [InlineData("RabbitMq:ConnectionString", UnreachableEndpoints.RabbitMq)]
    public async Task Ready_NonCriticalDependencyDown_ReturnsDegraded(string key, string connectionString)
    {
        await using var factory = containers.CreateFactory(new Dictionary<string, string?> { [key] = connectionString });

        var (status, body) = await GetAsync(factory, "/health/ready");

        status.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe("Degraded");
    }

    [Fact]
    public async Task ReadyDetails_NonDevelopment_Returns404()
    {
        await using var factory = containers.CreateFactory();

        var (status, _) = await GetAsync(factory, "/health/ready/details");

        status.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<(HttpStatusCode Status, string Body)> GetAsync(AutoMarketApiFactory factory, string path)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        return (response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
