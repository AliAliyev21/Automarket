using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AutoMarket.Identity.Infrastructure.Tokens;

// SEC-AUTH-04: HS256, kid ilə açar; claim-lər yalnız sub, role, jti, iss, aud, exp, iat (email və telefon yoxdur)
internal sealed class JwtAccessTokenIssuer(
    JwtKeyRing keyRing,
    IOptions<JwtOptions> options,
    IIdGenerator ids,
    TimeProvider time) : IAccessTokenIssuer
{
    public const string SubjectClaim = JwtRegisteredClaimNames.Sub;
    public const string RoleClaim = "role";

    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    public AccessToken Issue(Guid userId, IReadOnlyCollection<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);

        var settings = options.Value;
        var now = time.GetUtcNow();
        var expiresAt = now + settings.AccessTokenLifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = keyRing.GetSigningCredentials(),
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [SubjectClaim] = userId.ToString(),
                [JwtRegisteredClaimNames.Jti] = ids.NewId().ToString(),
                [RoleClaim] = roles.ToArray(),
            },
        };

        return new AccessToken(Handler.CreateToken(descriptor), expiresAt);
    }
}
