using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Identity.Infrastructure.Persistence.Repositories;

// ExecuteUpdate interceptor-u keçir: bu əməliyyatlar domen hadisəsi yaratmır (CONVENTIONS §7.3). Handler-in açdığı
// transaksiyada işləyir
internal sealed class RefreshTokenRepository(IdentityDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(byte[] tokenHash, CancellationToken cancellationToken) =>
        db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.RefreshTokens
            .Where(token => token.UserId == userId
                && token.RevokedAt == null
                && token.ExpiresAt > now
                && token.AbsoluteExpiresAt > now)
            .OrderBy(token => token.CreatedAt)
            .ThenBy(token => token.Id)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryRevokeForRotationAsync(Guid tokenId, Guid replacedById, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var updated = await db.RefreshTokens
            .Where(token => token.Id == tokenId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedReason, RefreshTokenRevocationReason.Rotated)
                    .SetProperty(token => token.ReplacedById, replacedById),
                cancellationToken);

        return updated == 1;
    }

    public Task<int> RevokeAllActiveAsync(Guid userId, RefreshTokenRevocationReason reason, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedReason, reason),
                cancellationToken);
}
