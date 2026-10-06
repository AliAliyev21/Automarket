namespace AutoMarket.Identity.Contracts.Events;

// ARCHITECTURE §5.4-dəki növlər. AccountDeleted FR-ACC-02 ilə göndərilməyə başlayır
public enum AuthEmailKind
{
    ConfirmEmail,
    ResetPassword,
    PasswordChanged,
    LockedOut,
    RegistrationAttemptOnExistingAccount,
    AccountDeleted,
}
