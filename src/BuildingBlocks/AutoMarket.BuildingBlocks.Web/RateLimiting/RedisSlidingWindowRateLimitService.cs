using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.RateLimiting;
using AutoMarket.BuildingBlocks.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AutoMarket.BuildingBlocks.Web.RateLimiting;

// ADR-0011: sliding window counter. Cari və əvvəlki sabit pəncərənin sayğacları saxlanılır, təxmini say
// prev × (1 − elapsed/window) + curr düsturu ilə bir Lua skriptində atomik hesablanır. Açar başına O(1) yaddaş, TTL = 2 × pəncərə.
// Redis əlçatmaz olduqda lokal in-memory limiter-ə keçilir (eyni limitlər, instansiya üzrə) və Warning yazılır
internal sealed partial class RedisSlidingWindowRateLimitService(
    IConnectionMultiplexer redis,
    IOptions<RateLimitingOptions> options,
    TimeProvider time,
    ILogger<RedisSlidingWindowRateLimitService> logger) : IRateLimitService, IDisposable
{
    // StackExchange.Redis skripti SHA ilə keşləyir və EVALSHA istifadə edir
    private const string Script = """
        local current = tonumber(redis.call('GET', KEYS[1]) or '0')
        local previous = tonumber(redis.call('GET', KEYS[2]) or '0')
        local limit = tonumber(ARGV[1])
        local windowMs = tonumber(ARGV[2])
        local elapsedMs = tonumber(ARGV[3])
        local estimated = previous * ((windowMs - elapsedMs) / windowMs) + current
        if estimated + 1 > limit then
            return 0
        end
        redis.call('INCR', KEYS[1])
        redis.call('PEXPIRE', KEYS[1], windowMs * 2)
        return 1
        """;

    private readonly ConcurrentDictionary<string, PartitionedRateLimiter<string>> _fallbackLimiters = new(StringComparer.Ordinal);
    private int _fallbackActive;

    public async Task<RateLimitDecision> TryAcquireAsync(string rule, string partitionKey, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var limit = settings.GetRule(rule);

        var windowMs = (long)limit.Window.TotalMilliseconds;
        var nowMs = time.GetUtcNow().ToUnixTimeMilliseconds();
        var windowIndex = nowMs / windowMs;
        var elapsedMs = nowMs % windowMs;

        var keyBase = $"{settings.KeyPrefix}:{rule}:{partitionKey}:";
        RedisKey[] keys =
        [
            keyBase + windowIndex.ToString(CultureInfo.InvariantCulture),
            keyBase + (windowIndex - 1).ToString(CultureInfo.InvariantCulture),
        ];
        RedisValue[] values = [limit.PermitLimit, windowMs, elapsedMs];

        try
        {
            var result = await redis.GetDatabase().ScriptEvaluateAsync(Script, keys, values);
            MarkRedisAvailable();

            return (int)result == 1
                ? RateLimitDecision.Allowed
                : new RateLimitDecision(false, TimeSpan.FromMilliseconds(windowMs - elapsedMs));
        }
        catch (Exception exception) when (exception is RedisConnectionException or RedisTimeoutException)
        {
            MarkFallback(exception);
            return AcquireFallback(rule, limit, partitionKey);
        }
    }

    public void Dispose()
    {
        foreach (var limiter in _fallbackLimiters.Values)
        {
            limiter.Dispose();
        }
    }

    private RateLimitDecision AcquireFallback(string rule, RateLimitRule limit, string partitionKey)
    {
        var limiter = _fallbackLimiters.GetOrAdd(rule, _ => PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = limit.PermitLimit,
                Window = limit.Window,
                SegmentsPerWindow = 4,
                QueueLimit = 0,
                AutoReplenishment = true,
            })));

        using var lease = limiter.AttemptAcquire(partitionKey);
        if (lease.IsAcquired)
        {
            return RateLimitDecision.Allowed;
        }

        return new RateLimitDecision(false, lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) ? retryAfter : limit.Window);
    }

    private void MarkFallback(Exception exception)
    {
        if (Interlocked.Exchange(ref _fallbackActive, 1) == 0)
        {
            LogFallbackActivated(logger, exception);
        }
    }

    private void MarkRedisAvailable()
    {
        if (Interlocked.Exchange(ref _fallbackActive, 0) == 1)
        {
            LogFallbackDeactivated(logger);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Redis is unavailable, rate limiting falls back to in-memory limiters")]
    private static partial void LogFallbackActivated(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Redis is available again, distributed rate limiting restored")]
    private static partial void LogFallbackDeactivated(ILogger logger);
}
