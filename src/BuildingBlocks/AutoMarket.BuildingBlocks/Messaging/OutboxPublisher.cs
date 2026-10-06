using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AutoMarket.BuildingBlocks.Messaging;

// ARCHITECTURE §5.2: modulun outbox-unu oxuyur (SKIP LOCKED — bir neçə instansiya eyni sətri göndərmir),
// publisher confirms ilə RabbitMQ-ya göndərir, təsdiqdən sonra processed_at yazır. Çatdırılma at-least-once-dır
internal sealed partial class OutboxPublisher<TContext>(
    IServiceScopeFactory scopeFactory,
    RabbitMqConnectionProvider connections,
    IOptions<MessagingOptions> options,
    TimeProvider time,
    ILogger<OutboxPublisher<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    private const int MaxErrorLength = 1000;

    private IChannel? _channel;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await CloseChannelAsync();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value.Outbox;

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay;
            try
            {
                var published = await PublishBatchAsync(settings.BatchSize, stoppingToken);
                delay = published >= settings.BatchSize ? TimeSpan.Zero
                    : published > 0 ? settings.PollInterval
                    : settings.IdleDelay;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Dövr sərhədi: DB və ya broker əlçatmazdır, növbəti dövrdə yenidən cəhd olunur (CONVENTIONS §5.3)
                LogCycleFailed(logger, exception, typeof(TContext).Name);
                await CloseChannelAsync();
                delay = options.Value.ReconnectDelay;
            }

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, time, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task<int> PublishBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TContext>();

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Schema adı koddakı sabitdir (istifadəçi inputu deyil), qalan dəyərlər parametrləşdirilir (SEC-INP-04)
        var sql = FormattableStringFactory.Create(
            $"SELECT * FROM \"{db.Model.GetDefaultSchema()}\".outbox WHERE processed_at IS NULL ORDER BY occurred_at LIMIT {{0}} FOR UPDATE SKIP LOCKED",
            batchSize);
        var messages = await db.Set<OutboxMessage>().FromSql(sql).ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return 0;
        }

        var channel = await GetChannelAsync(cancellationToken);
        var published = 0;

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(channel, message, cancellationToken);
                message.ProcessedAt = time.GetUtcNow();
                published++;
            }
            catch (PublishReturnException)
            {
                // Bu routing key-ə bind olunmuş queue yoxdur (məs. MVP-də consumer-i olmayan UserRegistered, ARCHITECTURE §5.4)
                message.ProcessedAt = time.GetUtcNow();
                LogUnrouted(logger, message.Type, message.Id);
            }
            catch (RabbitMQClientException exception)
            {
                message.Attempts++;
                message.LastError = Truncate($"{exception.GetType().Name}: {exception.Message}");
                LogPublishFailed(logger, exception, message.Type, message.Id, message.Attempts);
                await CloseChannelAsync();
                break;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return published;
    }

    private static async Task PublishAsync(IChannel channel, OutboxMessage message, CancellationToken cancellationToken)
    {
        using var payload = JsonDocument.Parse(message.Payload);
        var envelope = new MessageEnvelope(
            message.Id,
            message.Type,
            message.Version,
            message.OccurredAt,
            message.CorrelationId,
            message.TraceParent,
            payload.RootElement);

        var properties = new BasicProperties
        {
            MessageId = message.Id.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            CorrelationId = message.CorrelationId,
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope, MessagingJson.Options);

        // Publisher confirms + mandatory: təsdiq gəlməyibsə və ya nack olubsa exception atılır (ARCHITECTURE §5.2)
        await channel.BasicPublishAsync(
            RabbitMqTopology.EventsExchange,
            message.Type,
            mandatory: true,
            properties,
            body,
            cancellationToken);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true } channel)
        {
            return channel;
        }

        await CloseChannelAsync();

        var connection = await connections.GetConnectionAsync(cancellationToken);
        _channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await RabbitMqTopology.DeclareExchangesAsync(_channel, cancellationToken);

        return _channel;
    }

    private async Task CloseChannelAsync()
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            await _channel.DisposeAsync();
        }
        catch (RabbitMQClientException)
        {
            // Bağlantı artıq qırılıb; kanal yenisi ilə əvəz olunacaq
        }

        _channel = null;
    }

    private static string Truncate(string value) => value.Length > MaxErrorLength ? value[..MaxErrorLength] : value;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox publishing cycle for {Context} failed")]
    private static partial void LogCycleFailed(ILogger logger, Exception exception, string context);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publishing {MessageType} {MessageId} failed, attempt {Attempts}")]
    private static partial void LogPublishFailed(ILogger logger, Exception exception, string messageType, Guid messageId, int attempts);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{MessageType} {MessageId} has no bound queue and was dropped by the broker")]
    private static partial void LogUnrouted(ILogger logger, string messageType, Guid messageId);
}
