using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application;
using AutoMarket.Identity.Application.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Infrastructure.Caching;

// SEC-AUTH-06 (ARCHITECTURE §7.2, §8.7): status HybridCache-dədir, açar user-status:{id}. Block/unblock/rol dəyişikliyi
// açarı L2-dən və cari instansiyanın L1-indən silir; digər instansiyalarda köhnə dəyər ən çox L1 TTL qədər qalır
internal sealed class CachedUserStatusReader(
    HybridCache cache,
    IUserQueries queries,
    IOptions<IdentityOptions> options) : IUserStatusReader, IUserStatusCache
{
    public async Task<UserAccessStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        var settings = options.Value.StatusCache;
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = settings.Expiration,
            LocalCacheExpiration = settings.LocalExpiration,
        };

        return await cache.GetOrCreateAsync(
            Key(userId),
            (queries, userId),
            static (state, token) => new ValueTask<UserAccessStatus>(state.queries.GetAccessStatusAsync(state.userId, token)),
            entryOptions,
            cancellationToken: cancellationToken);
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken) =>
        cache.RemoveAsync(Key(userId), cancellationToken).AsTask();

    private static string Key(Guid userId) => $"user-status:{userId}";
}
