namespace AutoMarket.Identity.Domain.Tokens;

// ARCHITECTURE §7.2 SEC-AUTH-05: ləğvetmə səbəbləri
internal enum RefreshTokenRevocationReason
{
    Rotated,
    ReuseDetected,
    SessionLimit,

    // FR-AUTH-05
    Logout,
    LogoutAll,

    // FR-AUTH-06 AC4, FR-AUTH-07 AC2
    PasswordReset,
    PasswordChanged,

    // FR-ADM-01 AC2, FR-ADM-02 AC2
    Blocked,
    RoleDowngraded,
}
