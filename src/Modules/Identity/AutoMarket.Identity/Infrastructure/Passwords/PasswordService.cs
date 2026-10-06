using System.Security.Cryptography;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace AutoMarket.Identity.Infrastructure.Passwords;

// SEC-AUTH-02: standart PasswordHasher (V3, PBKDF2-HMAC-SHA512, iterasiya konfiqurasiyadan). Müqayisə Identity daxilində sabit vaxtdadır
internal sealed class PasswordService(IPasswordHasher<User> hasher, DummyPasswordHash dummyHash) : IPasswordService
{
    private const int SecurityStampBytes = 20;

    public PasswordCheckResult Verify(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            dummyHash.Verify(hasher, password);
            return PasswordCheckResult.Failed;
        }

        return hasher.VerifyHashedPassword(user, user.PasswordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed,
        };
    }

    public void SimulateVerification(string password) => dummyHash.Verify(hasher, password);

    public void Rehash(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.PasswordHash = hasher.HashPassword(user, password);
    }

    // Security stamp yenilənir (Identity semantikası: etimadnamə dəyişdi)
    public void SetPassword(User user, string password)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.PasswordHash = hasher.HashPassword(user, password);
        user.SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(SecurityStampBytes));
    }
}
