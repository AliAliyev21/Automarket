using System.Security.Cryptography;
using System.Text;

namespace AutoMarket.BuildingBlocks.Security;

// ADR-0011 §5: email açarı Redis-də xam saxlanılmır, normallaşdırılmış email-in SHA-256 hash-i istifadə olunur
public static class RateLimitKeys
{
    public static string ForEmail(string normalizedEmail)
    {
        ArgumentNullException.ThrowIfNull(normalizedEmail);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)));
    }

    public static string ForUser(Guid userId) => userId.ToString("N");
}
