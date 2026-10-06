using System.Security.Cryptography;
using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Persistence;
using AutoMarket.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(ContainersFixture))]

namespace AutoMarket.IntegrationTests.Infrastructure;

// Konteynerlər assembly üçün bir dəfə qaldırılır, migration-lar bir dəfə tətbiq olunur (ARCHITECTURE §10.1).
// Image-lər docker-compose.yml ilə eynidir
public sealed class ContainersFixture : IAsyncLifetime
{
    public const string AllowedOrigin = "https://app.test";

    // Ömrü test qədər olan konteynerin parolu; placeholder deyil ki, ValidateOnStart keçsin
    private const string RedisPassword = "integration-tests-redis";

    // Default limitlər testlərin bir-birini bloklamaması üçün yüksəkdir; rate limit testləri öz limitlərini verir
    private const string HighPermitLimit = "100000";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("automarket")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine")
        .WithCommand("--requirepass", RedisPassword)
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management-alpine")
        .Build();

    // Test prosesi üçün təsadüfi imza açarı (SEC-SEC-03: ≥ 256 bit)
    private readonly string _jwtKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public string RedisConnectionString => $"{_redis.GetConnectionString()},password={RedisPassword}";

    public string RabbitMqConnectionString => _rabbitMq.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync(), _rabbitMq.StartAsync());

        await using var factory = CreateFactory();
        foreach (var migrator in factory.Services.GetServices<IModuleMigrator>())
        {
            await migrator.MigrateAsync(CancellationToken.None);
        }

        // Data Protection açar halqası ilk factory-də yaradılır ki, bütün factory-lər eyni açarı bazadan oxusun
        // (bir factory-nin şifrələdiyi tokeni başqasının consumer-i açır)
        _ = factory.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("warmup").Protect("warmup");
    }

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
            ["Jwt:SigningKeys:0:Key"] = _jwtKey,
            ["Cors:AllowedOrigins:0"] = AllowedOrigin,
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "1025",
            ["Smtp:Security"] = "None",
            ["Notifications:Links:ConfirmEmailUrl"] = $"{AllowedOrigin}/confirm-email",
            ["Messaging:Outbox:PollInterval"] = "00:00:00.100",
            ["Messaging:Outbox:IdleDelay"] = "00:00:00.200",
            ["Messaging:ReconnectDelay"] = "00:00:01",

            // Hər factory-nin öz rate limit sayğacları var (eyni Redis), testlər bir-birinə təsir etmir
            ["RateLimiting:KeyPrefix"] = $"rl-{Guid.CreateVersion7():N}",
        };

        foreach (var rule in RateLimitRules.All)
        {
            settings[$"RateLimiting:Rules:{rule}:PermitLimit"] = HighPermitLimit;
        }

        foreach (var (key, value) in overrides ?? new Dictionary<string, string?>())
        {
            settings[key] = value;
        }

        return new AutoMarketApiFactory(settings);
    }
}
