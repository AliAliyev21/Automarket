using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoMarket.BuildingBlocks.Messaging;

public static class MessagingServiceCollectionExtensions
{
    // RabbitMQ ConnectionFactory Host-da konfiqurasiyadan qurulur (connection string secret-dir)
    public static IServiceCollection AddMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .Validate(o => o.RetryDelays.Count > 0 && o.RetryDelays.All(d => d > TimeSpan.Zero), $"{MessagingOptions.SectionName}:RetryDelays must contain positive delays.")
            .Validate(o => o.Outbox.BatchSize > 0, $"{MessagingOptions.SectionName}:Outbox:BatchSize must be positive.")
            .Validate(o => o.Outbox.PollInterval > TimeSpan.Zero && o.Outbox.IdleDelay > TimeSpan.Zero, $"{MessagingOptions.SectionName}:Outbox delays must be positive.")
            .Validate(o => o.Consumer.Prefetch > 0, $"{MessagingOptions.SectionName}:Consumer:Prefetch must be positive.")
            .Validate(o => o.ReconnectDelay > TimeSpan.Zero, $"{MessagingOptions.SectionName}:ReconnectDelay must be positive.")
            .ValidateOnStart();

        services.TryAddSingleton<RabbitMqConnectionProvider>();

        return services;
    }

    public static IServiceCollection AddOutboxPublisher<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddHostedService<OutboxPublisher<TContext>>();
        return services;
    }

    // Queue adı: <modul>.events (ARCHITECTURE §5.3); inbox-da consumer adı kimi də istifadə olunur
    public static IServiceCollection AddIntegrationEventConsumer<TContext>(
        this IServiceCollection services,
        string queueName,
        Action<ConsumerBuilder<TContext>> configure)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configure);

        var definition = new ConsumerDefinition<TContext>(queueName);
        configure(new ConsumerBuilder<TContext>(services, definition));

        services.AddSingleton(definition);
        services.AddHostedService<RabbitMqConsumerHost<TContext>>();

        return services;
    }
}
