using AutoMarket.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Options;

namespace AutoMarket.IntegrationTests;

// SEC-SEC-03: məcburi secret olmadan və ya placeholder dəyərlə proses işə düşmür
public sealed class ConfigurationTests(ContainersFixture containers)
{
    [Fact]
    public async Task Startup_MissingPostgresConnectionString_FailsValidation()
    {
        await using var factory = containers.CreateFactory(new Dictionary<string, string?>
        {
            ["Postgres:ConnectionString"] = string.Empty,
        });

        var exception = Should.Throw<OptionsValidationException>(() => factory.CreateClient());

        exception.Message.ShouldContain("Postgres:ConnectionString is required.");
    }

    [Theory]
    [InlineData("Postgres:ConnectionString", "Host=localhost;Database=automarket;Username=automarket;Password=change-me-postgres")]
    [InlineData("Redis:ConnectionString", "localhost:6379,password=change-me-redis")]
    [InlineData("RabbitMq:ConnectionString", "amqp://automarket:change-me-rabbitmq@localhost:5672/")]
    public async Task Startup_PlaceholderPassword_FailsValidation(string key, string connectionString)
    {
        await using var factory = containers.CreateFactory(new Dictionary<string, string?> { [key] = connectionString });

        var exception = Should.Throw<OptionsValidationException>(() => factory.CreateClient());

        exception.Message.ShouldContain(key);
        exception.Message.ShouldNotContain("change-me");
    }
}
