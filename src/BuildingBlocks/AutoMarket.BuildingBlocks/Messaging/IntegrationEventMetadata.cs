using System.Collections.Concurrent;
using System.Reflection;

namespace AutoMarket.BuildingBlocks.Messaging;

public static class IntegrationEventMetadata
{
    private static readonly ConcurrentDictionary<Type, IntegrationEventAttribute> Cache = new();

    public static IntegrationEventAttribute Of(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return Cache.GetOrAdd(eventType, static type =>
            type.GetCustomAttribute<IntegrationEventAttribute>()
            ?? throw new InvalidOperationException($"Integration event {type.Name} has no [IntegrationEvent] attribute."));
    }

    public static IntegrationEventAttribute Of<TEvent>()
        where TEvent : IIntegrationEvent => Of(typeof(TEvent));
}
