using Microsoft.Extensions.Options;
using Npgsql;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace AutoMarket.Api.Configuration;

// Asılılıqların konfiqurasiyası və client-ləri. Məcburi dəyər olmadıqda proses işə düşmür (SEC-SEC-03, ARCHITECTURE §8.6)
internal static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<PostgresOptions, PostgresOptionsValidator>(configuration, PostgresOptions.SectionName);
        services.AddValidatedOptions<RedisOptions, RedisOptionsValidator>(configuration, RedisOptions.SectionName);
        services.AddValidatedOptions<RabbitMqOptions, RabbitMqOptionsValidator>(configuration, RabbitMqOptions.SectionName);

        services.AddOptions<DependencyHealthCheckOptions>()
            .Bind(configuration.GetSection(DependencyHealthCheckOptions.SectionName))
            .Validate(options => options.Timeout > TimeSpan.Zero, $"{DependencyHealthCheckOptions.SectionName}:Timeout must be positive.")
            .ValidateOnStart();

        services.AddSingleton(serviceProvider =>
            new NpgsqlDataSourceBuilder(serviceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value.ConnectionString)
                .UseLoggerFactory(serviceProvider.GetRequiredService<ILoggerFactory>())
                .Build());

        // AbortOnConnectFail = false: Redis əlçatmaz olsa da tətbiq işləyir, bağlantı fonda bərpa olunur (ARCHITECTURE §8.7)
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = ConfigurationOptions.Parse(serviceProvider.GetRequiredService<IOptions<RedisOptions>>().Value.ConnectionString);
            options.AbortOnConnectFail = false;
            options.LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return ConnectionMultiplexer.Connect(options);
        });

        // Paylaşılan bağlantı BuildingBlocks.Messaging-dəki RabbitMqConnectionProvider tərəfindən açılır (ADR-0005)
        services.AddSingleton(serviceProvider => new ConnectionFactory
        {
            Uri = new Uri(serviceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value.ConnectionString),
            ClientProvidedName = "automarket-api",
        });

        return services;
    }

    private static void AddValidatedOptions<TOptions, TValidator>(this IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        services.AddOptions<TOptions>().Bind(configuration.GetSection(sectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();
    }
}
