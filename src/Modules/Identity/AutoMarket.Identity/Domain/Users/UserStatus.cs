namespace AutoMarket.Identity.Domain.Users;

// REQUIREMENTS 2.1: User rolu email-i təsdiqlənmiş istifadəçidir. Blocked və Deleted mərhələ 3b-də əlavə olunur
internal enum UserStatus
{
    Unconfirmed,
    Active,
}
