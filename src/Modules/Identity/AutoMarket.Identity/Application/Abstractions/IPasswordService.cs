using AutoMarket.Identity.Domain.Users;

namespace AutoMarket.Identity.Application.Abstractions;

// SEC-AUTH-02: Identity-nin standart PasswordHasher-i (PBKDF2-HMAC-SHA512, ADR-0003)
internal interface IPasswordService
{
    public PasswordCheckResult Verify(User user, string password);

    // SEC-AUTH-08: hesab olmadıqda dummy hash yoxlanılır ki, cavab müddəti eyni olsun
    public void SimulateVerification(string password);

    // Saxlanılmış hash-in iterasiya sayı konfiqurasiyadakından azdırsa login zamanı yenilənir
    public void Rehash(User user, string password);

    // FR-AUTH-06/07: yeni hash və yeni security stamp. Siyasət yoxlaması (IPasswordPolicy) əvvəldən aparılır
    public void SetPassword(User user, string password);
}

internal enum PasswordCheckResult
{
    Failed,
    Success,
    SuccessRehashNeeded,
}
