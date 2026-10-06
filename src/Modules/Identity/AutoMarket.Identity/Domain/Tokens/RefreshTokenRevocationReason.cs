namespace AutoMarket.Identity.Domain.Tokens;

// ARCHITECTURE §7.2 SEC-AUTH-05: ləğvetmə səbəbləri. Logout, PasswordChanged, Blocked və s. mərhələ 3b-də əlavə olunur
internal enum RefreshTokenRevocationReason
{
    Rotated,
    ReuseDetected,
    SessionLimit,
}
