using System.Security.Cryptography;
using System.Text;

namespace AutoMarket.BuildingBlocks.Security;

// Serverdə tokenin yalnız SHA-256 hash-i saxlanılır, müqayisə sabit vaxtda aparılır (SEC-AUTH-05, SEC-AUTH-07)
public static class TokenHasher
{
    public static byte[] Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    public static bool Matches(string token, byte[] expectedHash)
    {
        ArgumentNullException.ThrowIfNull(expectedHash);
        return CryptographicOperations.FixedTimeEquals(Hash(token), expectedHash);
    }
}
