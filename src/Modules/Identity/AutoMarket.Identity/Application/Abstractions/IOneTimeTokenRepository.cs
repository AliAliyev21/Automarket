using AutoMarket.Identity.Domain.Tokens;

namespace AutoMarket.Identity.Application.Abstractions;

internal interface IOneTimeTokenRepository
{
    public Task<OneTimeToken?> FindByHashAsync(byte[] tokenHash, CancellationToken cancellationToken);

    public void Add(OneTimeToken token);

    // FR-AUTH-02 AC3: yeni token eyni məqsədli köhnə tokenləri etibarsız edir
    public Task RevokeActiveAsync(Guid userId, OneTimeTokenPurpose purpose, DateTimeOffset now, CancellationToken cancellationToken);
}
