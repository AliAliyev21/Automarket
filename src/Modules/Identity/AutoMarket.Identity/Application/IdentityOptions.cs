using AutoMarket.Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Application;

// Identity modulunun limitləri və müddətləri (REQUIREMENTS FR-AUTH, SEC-AUTH). Dəyərlər appsettings.json-dadır, kodda sabit yoxdur
internal sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    // OWASP / ADR-0003: PBKDF2-HMAC-SHA512 üçün aşağı hədd
    public const int MinimumIterationCount = 210_000;

    public PasswordSettings Password { get; set; } = new();

    public LockoutSettings Lockout { get; set; } = new();

    public TokenSettings Tokens { get; set; } = new();

    public RefreshTokenSettings RefreshTokens { get; set; } = new();

    public StatusCacheSettings StatusCache { get; set; } = new();

    internal sealed class PasswordSettings
    {
        public int IterationCount { get; set; }

        public int MinLength { get; set; }

        public int MaxLength { get; set; }
    }

    internal sealed class LockoutSettings
    {
        public int MaxFailedAttempts { get; set; }

        public TimeSpan FailureWindow { get; set; }

        public TimeSpan BaseDuration { get; set; }

        public TimeSpan MaxDuration { get; set; }

        public LockoutPolicy ToPolicy() => new(MaxFailedAttempts, FailureWindow, BaseDuration, MaxDuration);
    }

    internal sealed class TokenSettings
    {
        public TimeSpan EmailConfirmationLifetime { get; set; }

        // FR-AUTH-06 AC2: 1 saat
        public TimeSpan PasswordResetLifetime { get; set; }
    }

    internal sealed class RefreshTokenSettings
    {
        public TimeSpan SlidingLifetime { get; set; }

        public TimeSpan AbsoluteLifetime { get; set; }

        public int MaxSessions { get; set; }
    }

    // ARCHITECTURE §8.7: user-status:{id} — L2 (Redis) 5 dəq, L1 5 s. L1 TTL bloklamanın digər instansiyalarda gecikməsidir
    internal sealed class StatusCacheSettings
    {
        public TimeSpan Expiration { get; set; }

        public TimeSpan LocalExpiration { get; set; }
    }
}

internal sealed class IdentityOptionsValidator : IValidateOptions<IdentityOptions>
{
    private const string Section = IdentityOptions.SectionName;

    public ValidateOptionsResult Validate(string? name, IdentityOptions options)
    {
        var failures = new List<string>();

        if (options.Password.IterationCount < IdentityOptions.MinimumIterationCount)
        {
            failures.Add($"{Section}:Password:IterationCount must be at least {IdentityOptions.MinimumIterationCount}.");
        }

        // SEC-AUTH-01: minimum 10, maksimum 128 simvol
        if (options.Password.MinLength < 10 || options.Password.MaxLength < options.Password.MinLength)
        {
            failures.Add($"{Section}:Password length limits are invalid.");
        }

        var lockout = options.Lockout;
        if (lockout.MaxFailedAttempts <= 0 || lockout.FailureWindow <= TimeSpan.Zero
            || lockout.BaseDuration <= TimeSpan.Zero || lockout.MaxDuration < lockout.BaseDuration)
        {
            failures.Add($"{Section}:Lockout settings are invalid.");
        }

        if (options.Tokens.EmailConfirmationLifetime <= TimeSpan.Zero)
        {
            failures.Add($"{Section}:Tokens:EmailConfirmationLifetime must be positive.");
        }

        if (options.Tokens.PasswordResetLifetime <= TimeSpan.Zero)
        {
            failures.Add($"{Section}:Tokens:PasswordResetLifetime must be positive.");
        }

        var cache = options.StatusCache;
        if (cache.Expiration <= TimeSpan.Zero || cache.LocalExpiration <= TimeSpan.Zero || cache.LocalExpiration > cache.Expiration)
        {
            failures.Add($"{Section}:StatusCache settings are invalid.");
        }

        var refresh = options.RefreshTokens;
        if (refresh.SlidingLifetime <= TimeSpan.Zero || refresh.AbsoluteLifetime < refresh.SlidingLifetime || refresh.MaxSessions <= 0)
        {
            failures.Add($"{Section}:RefreshTokens settings are invalid.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
