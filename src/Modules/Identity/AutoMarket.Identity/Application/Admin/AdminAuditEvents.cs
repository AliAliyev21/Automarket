namespace AutoMarket.Identity.Application.Admin;

// SEC-LOG-03: bloklama, blokdan çıxarma, rol dəyişikliyi. Details-də email, şifrə, token yoxdur
internal static class AdminAuditEvents
{
    public const string UserBlocked = "admin.user_blocked";
    public const string UserUnblocked = "admin.user_unblocked";
    public const string RoleGranted = "admin.role_granted";
    public const string RoleRevoked = "admin.role_revoked";
    public const string AdminBootstrapped = "admin.bootstrapped";

    public const string ReasonKey = "reason";
    public const string RoleKey = "role";
}
