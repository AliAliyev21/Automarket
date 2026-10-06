namespace AutoMarket.Identity.Application.Auth;

// FR-AUTH-03 AC1, FR-AUTH-04 AC1: refresh token body-də qaytarılmır (SEC-NET-04)
internal sealed record AccessTokenResponse(string AccessToken, int ExpiresIn)
{
    public static AccessTokenResponse From(SessionTokens tokens) => new(tokens.AccessToken, tokens.AccessTokenExpiresInSeconds);
}
