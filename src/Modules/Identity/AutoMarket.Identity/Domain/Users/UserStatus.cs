namespace AutoMarket.Identity.Domain.Users;

// REQUIREMENTS 2.1: User rolu email-i təsdiqlənmiş və bloklanmamış istifadəçidir (R-04). Deleted FR-ACC-02 ilə əlavə olunur
internal enum UserStatus
{
    Unconfirmed,
    Active,
    Blocked,
}
