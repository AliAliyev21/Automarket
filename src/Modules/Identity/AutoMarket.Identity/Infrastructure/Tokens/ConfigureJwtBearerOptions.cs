using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AutoMarket.Identity.Infrastructure.Tokens;

// SEC-AUTH-04 (ARCHITECTURE §7.2): alqoritm serverdə sabitdir (none və alqoritm dəyişdirmə qəbul edilmir), issuer, audience,
// müddət və imza hər sorğuda yoxlanılır, clock skew ≤ 30 s, MapInboundClaims = false (sub olduğu kimi qalır)
internal sealed class ConfigureJwtBearerOptions(JwtKeyRing keyRing, IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        Configure(options);
    }

    public void Configure(JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var settings = jwtOptions.Value;

        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = settings.ClockSkew,
            NameClaimType = JwtAccessTokenIssuer.SubjectClaim,
            RoleClaimType = JwtAccessTokenIssuer.RoleClaim,
            IssuerSigningKeyResolver = (_, _, keyId, _) => keyRing.GetValidationKeys(keyId),
        };
    }
}
