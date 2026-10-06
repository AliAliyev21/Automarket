namespace AutoMarket.Identity.Application.Abstractions;

// SEC-AUTH-04: JWT, yalnız minimum claim-lər (sub, role, jti, iss, aud, exp, iat), şəxsi məlumat yoxdur
internal interface IAccessTokenIssuer
{
    public AccessToken Issue(Guid userId, IReadOnlyCollection<string> roles);
}

internal sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
