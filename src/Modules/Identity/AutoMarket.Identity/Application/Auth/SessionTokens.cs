using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth;

// Login və refresh nəticəsi. Api qatı access tokeni body-də, refresh tokeni yalnız cookie-də qaytarır (SEC-NET-04)
[LogRedact]
internal sealed record SessionTokens(
    string AccessToken,
    int AccessTokenExpiresInSeconds,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
