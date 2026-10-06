namespace AutoMarket.BuildingBlocks.Audit;

// audit.audit_log sətri (SEC-LOG-04). Dəyişdirilmir və silinmir: DB trigger-i UPDATE/DELETE-i rədd edir (ARCHITECTURE §7.2)
internal sealed class AuditLogEntry
{
    public Guid Id { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public required string EventType { get; init; }

    public Guid? ActorId { get; init; }

    public string? TargetType { get; init; }

    public string? TargetId { get; init; }

    public required string Result { get; init; }

    public string? Ip { get; init; }

    public string? UserAgent { get; init; }

    public string? CorrelationId { get; init; }

    public string? Details { get; init; }
}
