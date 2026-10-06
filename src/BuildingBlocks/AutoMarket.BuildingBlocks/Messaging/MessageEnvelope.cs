using System.Text.Json;

namespace AutoMarket.BuildingBlocks.Messaging;

// Bütün event-lər üçün envelope (ARCHITECTURE §5.4)
internal sealed record MessageEnvelope(
    Guid MessageId,
    string Type,
    int Version,
    DateTimeOffset OccurredAt,
    string? CorrelationId,
    string? TraceParent,
    JsonElement Payload);
