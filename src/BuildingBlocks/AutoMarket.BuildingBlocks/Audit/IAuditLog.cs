namespace AutoMarket.BuildingBlocks.Audit;

// SEC-LOG-03/04: qeyd biznes transaksiyası daxilində modulun outbox-una AuditRecorded kimi yazılır (ARCHITECTURE §5.2).
// Record SaveChanges-dən əvvəl çağırılır (CONVENTIONS §7.4)
public interface IAuditLog
{
    public void Record(AuditEntry entry);
}

// Details-də şəxsi məlumat (email, telefon, şifrə, token) olmur (SEC-LOG-04)
public sealed record AuditEntry(
    string EventType,
    string Result,
    Guid? ActorId = null,
    string? TargetType = null,
    string? TargetId = null,
    IReadOnlyDictionary<string, string>? Details = null);
