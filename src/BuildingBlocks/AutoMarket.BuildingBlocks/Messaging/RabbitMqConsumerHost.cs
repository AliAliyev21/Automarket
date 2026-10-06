using System.Runtime.CompilerServices;
using System.Text.Json;
using AutoMarket.BuildingBlocks.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AutoMarket.BuildingBlocks.Messaging;

// ARCHITECTURE §5.3: manual ack, prefetch; inbox sətri və handler bir DB transaksiyasındadır (idempotent consumer).
// Handler xətası → TTL-li retry queue (x-retry-count), cəhdlər bitəndə və ya oxunmayan mesaj → DLQ
internal sealed partial class RabbitMqConsumerHost<TContext>(
    ConsumerDefinition<TContext> definition,
    IServiceScopeFactory scopeFactory,
    RabbitMqConnectionProvider connections,
    IOptions<MessagingOptions> options,
    TimeProvider time,
    ILogger<RabbitMqConsumerHost<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeUntilChannelClosedAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Dövr sərhədi: broker əlçatmazdır və ya kanal qırılıb, yenidən qoşulma gözlənilir
                LogConsumerFailed(logger, exception, definition.QueueName);
            }

            try
            {
                await Task.Delay(options.Value.ReconnectDelay, time, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ConsumeUntilChannelClosedAsync(CancellationToken stoppingToken)
    {
        var connection = await connections.GetConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            stoppingToken);

        await RabbitMqTopology.DeclareConsumerQueueAsync(
            channel,
            definition.QueueName,
            definition.Handlers.Keys,
            options.Value.RetryDelays,
            stoppingToken);
        await channel.BasicQosAsync(0, options.Value.Consumer.Prefetch, global: false, stoppingToken);

        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        channel.ChannelShutdownAsync += (_, _) =>
        {
            closed.TrySetResult();
            return Task.CompletedTask;
        };

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, stoppingToken);
        await channel.BasicConsumeAsync(definition.QueueName, autoAck: false, consumer, stoppingToken);

        await closed.Task.WaitAsync(stoppingToken);
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        if (!TryRead(delivery, out var envelope, out var registration, out var integrationEvent))
        {
            await DeadLetterAsync(channel, delivery, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
            return;
        }

        CorrelationContext.Current = envelope.CorrelationId;
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = envelope.CorrelationId,
            ["MessageId"] = envelope.MessageId,
            ["MessageType"] = envelope.Type,
        });

        try
        {
            await ProcessAsync(envelope, registration, integrationEvent, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Ack olunmur: kanal bağlananda mesaj queue-ya qayıdır
            return;
        }
        catch (Exception exception)
        {
            await RetryOrDeadLetterAsync(channel, delivery, envelope, exception, stoppingToken);
        }

        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
    }

    // internal: inbox idempotentliyi integration testində birbaşa yoxlanılır
    internal async Task ProcessAsync(
        MessageEnvelope envelope,
        ConsumerRegistration registration,
        object integrationEvent,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Schema adı koddakı sabitdir, dəyərlər parametrləşdirilir (SEC-INP-04)
        var inserted = await db.Database.ExecuteSqlAsync(
            FormattableStringFactory.Create(
                $"INSERT INTO \"{db.Model.GetDefaultSchema()}\".inbox (message_id, consumer, processed_at) VALUES ({{0}}, {{1}}, {{2}}) ON CONFLICT DO NOTHING",
                envelope.MessageId,
                definition.QueueName,
                time.GetUtcNow()),
            cancellationToken);

        if (inserted == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            LogDuplicateSkipped(logger, envelope.Type, envelope.MessageId);
            return;
        }

        var context = new MessageContext(envelope.MessageId, envelope.OccurredAt, envelope.CorrelationId);
        await registration.InvokeAsync(scope.ServiceProvider, integrationEvent, context, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private bool TryRead(
        BasicDeliverEventArgs delivery,
        out MessageEnvelope envelope,
        out ConsumerRegistration registration,
        out object integrationEvent)
    {
        envelope = null!;
        registration = null!;
        integrationEvent = null!;

        try
        {
            var parsed = JsonSerializer.Deserialize<MessageEnvelope>(delivery.Body.Span, MessagingJson.Options);
            if (parsed is null || !definition.Handlers.TryGetValue(parsed.Type, out var found))
            {
                return false;
            }

            var payload = parsed.Payload.Deserialize(found.EventType, MessagingJson.Options);
            if (payload is null)
            {
                return false;
            }

            envelope = parsed;
            registration = found;
            integrationEvent = payload;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task RetryOrDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        MessageEnvelope envelope,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var attempt = GetRetryCount(delivery.BasicProperties) + 1;
        var retryDelays = options.Value.RetryDelays;

        if (attempt > retryDelays.Count)
        {
            LogDeadLettered(logger, exception, envelope.Type, envelope.MessageId, attempt - 1);
            await DeadLetterAsync(channel, delivery, cancellationToken);
            return;
        }

        LogRetryScheduled(logger, exception, envelope.Type, envelope.MessageId, attempt);
        var properties = CopyProperties(delivery.BasicProperties, attempt);
        await channel.BasicPublishAsync(
            RabbitMqTopology.RetryExchange,
            RabbitMqTopology.RetryQueue(definition.QueueName, attempt),
            mandatory: true,
            properties,
            delivery.Body,
            cancellationToken);
    }

    private async Task DeadLetterAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        LogUnreadableOrDeadLettered(logger, delivery.BasicProperties.Type, definition.QueueName);
        var properties = CopyProperties(delivery.BasicProperties, GetRetryCount(delivery.BasicProperties));
        await channel.BasicPublishAsync(
            RabbitMqTopology.RetryExchange,
            RabbitMqTopology.DeadLetterQueue(definition.QueueName),
            mandatory: true,
            properties,
            delivery.Body,
            cancellationToken);
    }

    private static BasicProperties CopyProperties(IReadOnlyBasicProperties source, int retryCount)
    {
        var headers = source.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(source.Headers);
        headers[RabbitMqTopology.RetryCountHeader] = retryCount;

        return new BasicProperties(source) { Headers = headers };
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties) =>
        properties.Headers?.TryGetValue(RabbitMqTopology.RetryCountHeader, out var value) == true
            ? value switch
            {
                int count => count,
                long count => (int)count,
                _ => 0,
            }
            : 0;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Consumer {Queue} stopped, reconnecting")]
    private static partial void LogConsumerFailed(ILogger logger, Exception exception, string queue);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Duplicate {MessageType} {MessageId} skipped by inbox")]
    private static partial void LogDuplicateSkipped(ILogger logger, string messageType, Guid messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handling {MessageType} {MessageId} failed, retry {Attempt} scheduled")]
    private static partial void LogRetryScheduled(ILogger logger, Exception exception, string messageType, Guid messageId, int attempt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Handling {MessageType} {MessageId} failed after {Attempts} retries, moved to DLQ")]
    private static partial void LogDeadLettered(ILogger logger, Exception exception, string messageType, Guid messageId, int attempts);

    [LoggerMessage(Level = LogLevel.Error, Message = "Message {MessageType} moved to DLQ of {Queue}")]
    private static partial void LogUnreadableOrDeadLettered(ILogger logger, string? messageType, string queue);
}
