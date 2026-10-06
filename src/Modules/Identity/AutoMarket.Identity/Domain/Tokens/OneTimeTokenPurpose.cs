namespace AutoMarket.Identity.Domain.Tokens;

internal enum OneTimeTokenPurpose
{
    EmailConfirmation,

    // FR-AUTH-06
    PasswordReset,
}
