namespace AutoMarket.Identity.Contracts.Events;

// ARCHITECTURE §5.4-dəki növlər. ResetPassword, PasswordChanged, AccountDeleted mərhələ 3b-də göndərilməyə başlayır
public enum AuthEmailKind
{
    ConfirmEmail,
    ResetPassword,
    PasswordChanged,
    LockedOut,
    RegistrationAttemptOnExistingAccount,
    AccountDeleted,
}
