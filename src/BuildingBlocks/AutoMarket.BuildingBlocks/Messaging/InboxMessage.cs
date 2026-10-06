namespace AutoMarket.BuildingBlocks.Messaging;

// İdempotent consumer: (message_id, consumer) unikal açardır (ARCHITECTURE §5.3)
public sealed class InboxMessage
{
    public Guid MessageId { get; init; }

    public required string Consumer { get; init; }

    public DateTimeOffset ProcessedAt { get; init; }
}
