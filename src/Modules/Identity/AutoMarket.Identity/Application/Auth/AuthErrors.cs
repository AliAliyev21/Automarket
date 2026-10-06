using AutoMarket.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.Identity.Application.Auth;

// Kodlar REQUIREMENTS 4.8-dəndir, hər kod yalnız burada təyin olunur (CONVENTIONS §5.4). Statuslar CONVENTIONS §6.3-dədir
internal static class AuthErrors
{
    // FR-AUTH-03 AC2: yanlış email və yanlış şifrə üçün eyni kod və mesaj
    public static readonly Error InvalidCredentials =
        new("INVALID_CREDENTIALS", "Invalid email or password.", StatusCodes.Status401Unauthorized);

    // FR-AUTH-03 AC3: yalnız şifrə düzgün olduqda
    public static readonly Error EmailNotConfirmed =
        new("EMAIL_NOT_CONFIRMED", "Email address is not confirmed.", StatusCodes.Status403Forbidden);

    // FR-AUTH-03 AC5: kilidin bitmə vaxtı göstərilmir
    public static readonly Error AccountLockedOut =
        new("ACCOUNT_LOCKED_OUT", "The account is temporarily locked.", StatusCodes.Status403Forbidden);

    // FR-AUTH-02 AC2
    public static readonly Error TokenInvalidOrExpired =
        new("TOKEN_INVALID_OR_EXPIRED", "The token is invalid or has expired.", StatusCodes.Status400BadRequest);

    // FR-AUTH-04 AC2
    public static readonly Error RefreshTokenReused =
        new("REFRESH_TOKEN_REUSED", "The refresh token has already been used. All sessions were revoked.", StatusCodes.Status401Unauthorized);
}
