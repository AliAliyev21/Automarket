using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AutoMarket.Identity.Infrastructure.Tokens;

// SEC-SEC-05 (ARCHITECTURE §7.2): imzalama aktiv açarla, yoxlama kid üzrə bütün retire olunmamış açarlarla aparılır
internal sealed class JwtKeyRing(IOptions<JwtOptions> options, TimeProvider time)
{
    private readonly IReadOnlyList<KeyEntry> _keys = [.. options.Value.SigningKeys.Select(key => new KeyEntry(
        new SymmetricSecurityKey(Convert.FromBase64String(key.Key)) { KeyId = key.KeyId },
        key.NotBefore,
        key.RetireAfter))];

    // Aktiv açar: imzalamağa başlama vaxtı ən gec olan və retire olunmamış açar
    public SigningCredentials GetSigningCredentials()
    {
        var now = time.GetUtcNow();
        var active = _keys
            .Where(key => key.CanSign(now))
            .OrderByDescending(key => key.NotBefore ?? DateTimeOffset.MinValue)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No active JWT signing key is configured.");

        return new SigningCredentials(active.Key, SecurityAlgorithms.HmacSha256);
    }

    public IEnumerable<SecurityKey> GetValidationKeys(string? keyId)
    {
        var now = time.GetUtcNow();
        return _keys
            .Where(key => !key.IsRetired(now) && string.Equals(key.Key.KeyId, keyId, StringComparison.Ordinal))
            .Select(key => key.Key);
    }

    private sealed record KeyEntry(SymmetricSecurityKey Key, DateTimeOffset? NotBefore, DateTimeOffset? RetireAfter)
    {
        public bool IsRetired(DateTimeOffset now) => RetireAfter is { } retireAfter && retireAfter <= now;

        public bool CanSign(DateTimeOffset now) => !IsRetired(now) && (NotBefore is null || NotBefore <= now);
    }
}
