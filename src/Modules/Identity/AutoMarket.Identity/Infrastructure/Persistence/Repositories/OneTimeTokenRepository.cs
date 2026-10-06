using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Identity.Infrastructure.Persistence.Repositories;

internal sealed class OneTimeTokenRepository(IdentityDbContext db) : IOneTimeTokenRepository
{
    public Task<OneTimeToken?> FindByHashAsync(byte[] tokenHash, CancellationToken cancellationToken) =>
        db.OneTimeTokens.FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public void Add(OneTimeToken token) => db.OneTimeTokens.Add(token);

    public async Task RevokeActiveAsync(Guid userId, OneTimeTokenPurpose purpose, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var active = await db.OneTimeTokens
            .Where(token => token.UserId == userId
                && token.Purpose == purpose
                && token.UsedAt == null
                && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.Revoke(now);
        }
    }
}
