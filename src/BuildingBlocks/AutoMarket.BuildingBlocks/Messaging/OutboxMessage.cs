namespace AutoMarket.BuildingBlocks.Messaging;

// Hər modulun <schema>.outbox cədvəli (ARCHITECTURE §5.2). Id = envelope messageId
public sealed class OutboxMessage
{
    public Guid Id { get; init; }

    public required string Type { get; init; }

    public int Version { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public string? CorrelationId { get; init; }

    public string? TraceParent { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    public string? LastError { get; set; }
}
