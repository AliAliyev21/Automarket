using AutoMarket.IntegrationTests.Infrastructure;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(ContainersFixture))]

namespace AutoMarket.IntegrationTests.Infrastructure;

// Konteynerlər assembly üçün bir dəfə qaldırılır (ARCHITECTURE §10.1). Image-lər docker-compose.yml ilə eynidir
public sealed class ContainersFixture : IAsyncLifetime
{
    // Ömrü test qədər olan konteynerin parolu; placeholder deyil ki, ValidateOnStart keçsin
    private const string RedisPassword = "integration-tests-redis";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("automarket")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine")
        .WithCommand("--requirepass", RedisPassword)
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management-alpine")
        .Build();

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public string RedisConnectionString => $"{_redis.GetConnectionString()},password={RedisPassword}";

    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    public async ValueTask InitializeAsync() =>
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), _rabbitMq.StartAsync());

    public async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    // Bütün asılılıqlar real konteynerlərə yönəlir; overrides ilə ayrı-ayrı dəyərlər dəyişdirilir
    public AutoMarketApiFactory CreateFactory(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Postgres:ConnectionString"] = PostgresConnectionString,
            ["Redis:ConnectionString"] = RedisConnectionString,
            ["RabbitMq:ConnectionString"] = RabbitMqConnectionString,
        };

        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            settings[key] = value;
        }

        return new AutoMarketApiFactory(settings);
    }
}
