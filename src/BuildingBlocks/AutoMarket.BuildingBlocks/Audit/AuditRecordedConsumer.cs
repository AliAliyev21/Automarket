using System.Text.Json;
using AutoMarket.BuildingBlocks.Messaging;

namespace AutoMarket.BuildingBlocks.Audit;

// Id = messageId: təkrar çatdırılmada inbox-dan əlavə ikinci qoruma (ARCHITECTURE §5.3)
internal sealed class AuditRecordedConsumer(AuditDbContext db) : IIntegrationEventConsumer<AuditRecorded>
{
    public Task ConsumeAsync(AuditRecorded integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);

        db.AuditLog.Add(new AuditLogEntry
        {
            Id = context.MessageId,
            OccurredAt = integrationEvent.OccurredAt,
            EventType = integrationEvent.EventType,
            ActorId = integrationEvent.ActorId,
            TargetType = integrationEvent.TargetType,
            TargetId = integrationEvent.TargetId,
            Result = integrationEvent.Result,
            Ip = integrationEvent.Ip,
            UserAgent = Truncate(integrationEvent.UserAgent, 512),
            CorrelationId = integrationEvent.CorrelationId,
            Details = integrationEvent.Details is null ? null : JsonSerializer.Serialize(integrationEvent.Details),
        });

        return Task.CompletedTask;
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;
}
