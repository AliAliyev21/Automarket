using AutoMarket.BuildingBlocks.Security;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Infrastructure.Tokens;

// SEC-AUTH-04, SEC-SEC-05: imza açarları yalnız user-secrets / environment variable ilə verilir (SEC-SEC-02).
// Bir neçə açar ola bilər: keçid dövründə köhnə və yeni açar paralel qəbul olunur
internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    // SEC-SEC-03: imza açarı ≥ 256 bit
    public const int MinimumKeySizeBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public TimeSpan AccessTokenLifetime { get; set; }

    public TimeSpan ClockSkew { get; set; }

    public IList<JwtSigningKeyOptions> SigningKeys { get; } = [];
}

internal sealed class JwtSigningKeyOptions
{
    public string KeyId { get; set; } = string.Empty;

    // base64
    public string Key { get; set; } = string.Empty;

    // Bu vaxtdan sonra imzalamaq üçün istifadə oluna bilər (null — dərhal)
    public DateTimeOffset? NotBefore { get; set; }

    // Bu vaxtdan sonra nə imzalamaq, nə də yoxlamaq üçün istifadə olunur
    public DateTimeOffset? RetireAfter { get; set; }
}

// Xəta mesajlarında açarın dəyəri yazılmır (SEC-LOG-01)
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const string Section = JwtOptions.SectionName;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{Section}:Issuer and {Section}:Audience are required.");
        }

        // SEC-AUTH-04: qısa ömür (15 dəq), saat sürüşməsi ≤ 30 saniyə
        if (options.AccessTokenLifetime <= TimeSpan.Zero || options.AccessTokenLifetime > TimeSpan.FromHours(1))
        {
            failures.Add($"{Section}:AccessTokenLifetime must be positive and at most one hour.");
        }

        if (options.ClockSkew < TimeSpan.Zero || options.ClockSkew > TimeSpan.FromSeconds(30))
        {
            failures.Add($"{Section}:ClockSkew must be between 0 and 30 seconds.");
        }

        if (options.SigningKeys.Count == 0)
        {
            failures.Add($"{Section}:SigningKeys must contain at least one key.");
        }

        for (var i = 0; i < options.SigningKeys.Count; i++)
        {
            var key = options.SigningKeys[i];
            var path = $"{Section}:SigningKeys:{i}";

            if (string.IsNullOrWhiteSpace(key.KeyId))
            {
                failures.Add($"{path}:KeyId is required.");
            }

            if (string.IsNullOrWhiteSpace(key.Key) || SecretValue.IsPlaceholder(key.Key) || !HasMinimumSize(key.Key))
            {
                failures.Add($"{path}:Key must be a non-placeholder base64 value of at least {MinimumKeySizeBits} bits.");
            }
        }

        if (options.SigningKeys.Select(key => key.KeyId).Distinct(StringComparer.Ordinal).Count() != options.SigningKeys.Count)
        {
            failures.Add($"{Section}:SigningKeys key ids must be unique.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private const int MinimumKeySizeBits = JwtOptions.MinimumKeySizeBytes * 8;

    private static bool HasMinimumSize(string base64)
    {
        var buffer = new byte[base64.Length];
        return Convert.TryFromBase64String(base64, buffer, out var written) && written >= JwtOptions.MinimumKeySizeBytes;
    }
}
