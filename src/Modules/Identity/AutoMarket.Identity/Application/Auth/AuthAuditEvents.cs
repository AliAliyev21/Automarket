namespace AutoMarket.Identity.Application.Auth;

// SEC-LOG-03: audit olunan auth hadisələri. Details-də email, şifrə, token yoxdur
internal static class AuthAuditEvents
{
    public const string UserRegistered = "auth.registered";
    public const string RegistrationAttemptOnExistingAccount = "auth.registration_attempt_existing";
    public const string EmailConfirmed = "auth.email_confirmed";
    public const string LoginSucceeded = "auth.login_succeeded";
    public const string LoginFailed = "auth.login_failed";
    public const string LockedOut = "auth.locked_out";
    public const string RefreshTokenReused = "auth.refresh_token_reused";

    public const string TargetUser = "user";

    public const string ResultSuccess = "success";
    public const string ResultFailure = "failure";

    public const string ReasonKey = "reason";
    public const string ReasonUnknownAccount = "unknown_account";
    public const string ReasonInvalidPassword = "invalid_password";
    public const string ReasonLockedOut = "locked_out";
    public const string ReasonEmailNotConfirmed = "email_not_confirmed";

    public static IReadOnlyDictionary<string, string> Reason(string reason) =>
        new Dictionary<string, string>(StringComparer.Ordinal) { [ReasonKey] = reason };
}
