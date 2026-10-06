using AutoMarket.BuildingBlocks.Messaging;

namespace AutoMarket.BuildingBlocks.Audit;

// ARCHITECTURE §5.4: bütün modulların outbox-undan Audit consumer-inə gedir. Payload PII-sizdir
[IntegrationEvent("audit.recorded", 1)]
public sealed record AuditRecorded(
    string EventType,
    Guid? ActorId,
    string? TargetType,
    string? TargetId,
    string Result,
    string? Ip,
    string? UserAgent,
    string? CorrelationId,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, string>? Details) : IIntegrationEvent;
