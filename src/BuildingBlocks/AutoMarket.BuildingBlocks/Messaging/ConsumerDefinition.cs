using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.Messaging;

// Bir modulun queue-su və onun routing key → handler xəritəsi (ARCHITECTURE §5.3: modul başına bir queue)
internal sealed class ConsumerDefinition<TContext>(string queueName)
    where TContext : DbContext
{
    private readonly Dictionary<string, ConsumerRegistration> _handlers = new(StringComparer.Ordinal);

    public string QueueName { get; } = queueName;

    public IReadOnlyDictionary<string, ConsumerRegistration> Handlers => _handlers;

    public void Add<TEvent, TConsumer>()
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        var routingKey = IntegrationEventMetadata.Of<TEvent>().RoutingKey;
        _handlers.Add(routingKey, new ConsumerRegistration(
            typeof(TEvent),
            (serviceProvider, integrationEvent, context, cancellationToken) => serviceProvider
                .GetRequiredService<TConsumer>()
                .ConsumeAsync((TEvent)integrationEvent, context, cancellationToken)));
    }
}

internal sealed record ConsumerRegistration(
    Type EventType,
    Func<IServiceProvider, object, MessageContext, CancellationToken, Task> InvokeAsync);

public sealed class ConsumerBuilder<TContext>
    where TContext : DbContext
{
    private readonly IServiceCollection _services;
    private readonly ConsumerDefinition<TContext> _definition;

    internal ConsumerBuilder(IServiceCollection services, ConsumerDefinition<TContext> definition)
    {
        _services = services;
        _definition = definition;
    }

    public ConsumerBuilder<TContext> Handle<TEvent, TConsumer>()
        where TEvent : IIntegrationEvent
        where TConsumer : class, IIntegrationEventConsumer<TEvent>
    {
        _services.AddScoped<TConsumer>();
        _definition.Add<TEvent, TConsumer>();
        return this;
    }
}
