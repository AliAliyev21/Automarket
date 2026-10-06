using AutoMarket.BuildingBlocks.Application;

namespace AutoMarket.BuildingBlocks.Audit;

// Scoped: sorğu daxilində yazılan qeydlər növbəti SaveChanges-də outbox interceptor-u tərəfindən götürülür
internal sealed class AuditLog(IRequestContext requestContext, TimeProvider time) : IAuditLog
{
    private readonly List<AuditRecorded> _pending = [];

    public void Record(AuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        _pending.Add(new AuditRecorded(
            entry.EventType,
            entry.ActorId,
            entry.TargetType,
            entry.TargetId,
            entry.Result,
            requestContext.IpAddress,
            requestContext.UserAgent,
            CorrelationContext.Current,
            time.GetUtcNow(),
            entry.Details));
    }

    public IReadOnlyList<AuditRecorded> DrainPending()
    {
        var drained = _pending.ToList();
        _pending.Clear();
        return drained;
    }
}
