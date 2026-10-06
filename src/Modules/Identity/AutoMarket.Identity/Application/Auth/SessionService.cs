using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Application.Auth;

// Login və refresh üçün ortaq məntiq: refresh token yaradılması və access token (CONVENTIONS §5.1: handler handler-i çağırmır)
internal sealed class SessionService(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IAccessTokenIssuer accessTokens,
    ISecureTokenGenerator tokenGenerator,
    IIdGenerator ids,
    IRequestContext requestContext,
    IOptions<IdentityOptions> options)
{
    // Yeni login: yeni token ailəsi. SEC-AUTH-05: ən çox MaxSessions aktiv sessiya, ən köhnəsi ləğv olunur
    public async Task<SessionTokens> StartAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var settings = options.Value.RefreshTokens;

        var active = await refreshTokens.GetActiveForUserAsync(userId, now, cancellationToken);
        var excess = active.Count - (settings.MaxSessions - 1);
        foreach (var oldest in active.Take(Math.Max(0, excess)))
        {
            oldest.Revoke(now, RefreshTokenRevocationReason.SessionLimit);
        }

        return await IssueAsync(ids.NewId(), userId, ids.NewId(), parentId: null, now + settings.AbsoluteLifetime, now, cancellationToken);
    }

    // Rotation: eyni ailədə yeni token, mütləq müddət ailədən götürülür (FR-AUTH-04 AC1, Q3)
    public Task<SessionTokens> RotateAsync(RefreshToken current, Guid newTokenId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(current);

        return IssueAsync(newTokenId, current.UserId, current.FamilyId, current.Id, current.AbsoluteExpiresAt, now, cancellationToken);
    }

    private async Task<SessionTokens> IssueAsync(
        Guid tokenId,
        Guid userId,
        Guid familyId,
        Guid? parentId,
        DateTimeOffset absoluteExpiresAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var rawRefreshToken = tokenGenerator.Generate();
        var refreshToken = RefreshToken.Issue(
            tokenId,
            userId,
            familyId,
            parentId,
            TokenHasher.Hash(rawRefreshToken),
            now,
            options.Value.RefreshTokens.SlidingLifetime,
            absoluteExpiresAt,
            requestContext.IpAddress,
            requestContext.UserAgent);
        refreshTokens.Add(refreshToken);

        var roles = await users.GetRolesAsync(userId, cancellationToken);
        var accessToken = accessTokens.Issue(userId, roles);

        return new SessionTokens(
            accessToken.Token,
            (int)(accessToken.ExpiresAt - now).TotalSeconds,
            rawRefreshToken,
            refreshToken.ExpiresAt);
    }
}
