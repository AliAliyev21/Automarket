namespace AutoMarket.Identity.Application.Abstractions;

// ARCHITECTURE §8.7: block/unblock/rol dəyişikliyi commit olunandan sonra user-status:{id} açarı silinir
internal interface IUserStatusCache
{
    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken);
}
