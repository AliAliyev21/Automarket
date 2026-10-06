using System.Diagnostics;
using System.Threading.RateLimiting;
using AutoMarket.BuildingBlocks.Application;

namespace AutoMarket.BuildingBlocks.Web.RateLimiting;

// ADR-0011 §2: ASP.NET Core rate limiting middleware-i üçün bir partition (məs. bir IP) üzrə limiter.
// Sayğac Redis-dədir, ona görə yalnız asinxron yol işləyir: sinxron cəhd həmişə uğursuz qaytarılır və
// middleware AcquireAsync-ə keçir
internal sealed class DistributedRateLimiter(IRateLimitService service, string rule, string partitionKey) : RateLimiter
{
    private long _lastUsedTimestamp = Stopwatch.GetTimestamp();

    // Middleware boş partition-ları bu dəyərə görə təmizləyir
    public override TimeSpan? IdleDuration => Stopwatch.GetElapsedTime(Interlocked.Read(ref _lastUsedTimestamp));

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount) =>
        permitCount == 0 ? SimpleLease.Acquired : SimpleLease.NotAcquiredSynchronously;

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _lastUsedTimestamp, Stopwatch.GetTimestamp());

        if (permitCount == 0)
        {
            return SimpleLease.Acquired;
        }

        var decision = await service.TryAcquireAsync(rule, partitionKey, cancellationToken);
        return decision.IsAllowed ? SimpleLease.Acquired : SimpleLease.Rejected(decision.RetryAfter);
    }
}

// Login: IP limiti + ümumi ConcurrencyLimiter (ADR-0003). Paylaşılan limiter burada dispose olunmur
internal sealed class CompositeRateLimiter(RateLimiter partitionLimiter, RateLimiter sharedLimiter) : RateLimiter
{
    public override TimeSpan? IdleDuration => partitionLimiter.IdleDuration;

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount) =>
        permitCount == 0 ? SimpleLease.Acquired : SimpleLease.NotAcquiredSynchronously;

    protected override async ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        var first = await partitionLimiter.AcquireAsync(permitCount, cancellationToken);
        if (!first.IsAcquired)
        {
            return first;
        }

        var second = await sharedLimiter.AcquireAsync(permitCount, cancellationToken);
        if (!second.IsAcquired)
        {
            first.Dispose();
            return second;
        }

        return new CompositeLease(first, second);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            partitionLimiter.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class CompositeLease(RateLimitLease first, RateLimitLease second) : RateLimitLease
    {
        public override bool IsAcquired => true;

        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                second.Dispose();
                first.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

internal sealed class SimpleLease : RateLimitLease
{
    private readonly TimeSpan? _retryAfter;

    private SimpleLease(bool isAcquired, TimeSpan? retryAfter)
    {
        IsAcquired = isAcquired;
        _retryAfter = retryAfter;
    }

    public static SimpleLease Acquired { get; } = new(true, null);

    public static SimpleLease NotAcquiredSynchronously { get; } = new(false, null);

    public override bool IsAcquired { get; }

    public override IEnumerable<string> MetadataNames => _retryAfter is null ? [] : [MetadataName.RetryAfter.Name];

    public static SimpleLease Rejected(TimeSpan retryAfter) => new(false, retryAfter);

    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        if (_retryAfter is { } retryAfter && metadataName == MetadataName.RetryAfter.Name)
        {
            metadata = retryAfter;
            return true;
        }

        metadata = null;
        return false;
    }
}
