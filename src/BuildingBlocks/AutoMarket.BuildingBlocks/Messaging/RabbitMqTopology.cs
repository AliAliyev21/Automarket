using RabbitMQ.Client;

namespace AutoMarket.BuildingBlocks.Messaging;

// ARCHITECTURE §5.3: topic exchange, hər consumer modul üçün quorum queue, TTL-li retry queue-ları və DLQ.
// Declare əməliyyatları idempotentdir və startup-da hər dəfə işlədilir
internal static class RabbitMqTopology
{
    public const string EventsExchange = "automarket.events";
    public const string RetryExchange = "automarket.retry";
    public const string RetryCountHeader = "x-retry-count";

    public static string RetryQueue(string queue, int attempt) => $"{queue}.retry.{attempt}";

    public static string DeadLetterQueue(string queue) => $"{queue}.dlq";

    public static async Task DeclareExchangesAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(EventsExchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(RetryExchange, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken);
    }

    public static async Task DeclareConsumerQueueAsync(
        IChannel channel,
        string queue,
        IEnumerable<string> routingKeys,
        IList<TimeSpan> retryDelays,
        CancellationToken cancellationToken)
    {
        await DeclareExchangesAsync(channel, cancellationToken);

        var queueArguments = new Dictionary<string, object?> { ["x-queue-type"] = "quorum" };
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, queueArguments, cancellationToken: cancellationToken);

        foreach (var routingKey in routingKeys)
        {
            await channel.QueueBindAsync(queue, EventsExchange, routingKey, cancellationToken: cancellationToken);
        }

        // TTL bitəndə mesaj default exchange vasitəsilə yalnız bu consumer-in queue-suna qayıdır
        for (var attempt = 1; attempt <= retryDelays.Count; attempt++)
        {
            var retryQueue = RetryQueue(queue, attempt);
            var retryArguments = new Dictionary<string, object?>
            {
                ["x-message-ttl"] = (long)retryDelays[attempt - 1].TotalMilliseconds,
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = queue,
            };

            await channel.QueueDeclareAsync(retryQueue, durable: true, exclusive: false, autoDelete: false, retryArguments, cancellationToken: cancellationToken);
            await channel.QueueBindAsync(retryQueue, RetryExchange, retryQueue, cancellationToken: cancellationToken);
        }

        var deadLetterQueue = DeadLetterQueue(queue);
        await channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false, queueArguments, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(deadLetterQueue, RetryExchange, deadLetterQueue, cancellationToken: cancellationToken);
    }
}
