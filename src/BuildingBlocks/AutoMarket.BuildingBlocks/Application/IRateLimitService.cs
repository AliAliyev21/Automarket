namespace AutoMarket.BuildingBlocks.Application;

// Body-dən gələn açarlar (email) və token sahibi üzrə limitlər handler daxilində yoxlanılır (ADR-0011 §6)
public interface IRateLimitService
{
    public Task<RateLimitDecision> TryAcquireAsync(string rule, string partitionKey, CancellationToken cancellationToken);
}

public sealed record RateLimitDecision(bool IsAllowed, TimeSpan RetryAfter)
{
    public static RateLimitDecision Allowed { get; } = new(true, TimeSpan.Zero);
}

// Qayda adları = konfiqurasiya açarları (RateLimiting:Rules:<ad>). Limitlər kodda deyil, konfiqurasiyadadır (SEC-RATE, ARCHITECTURE §7.4)
public static class RateLimitRules
{
    // SEC-RATE-01
    public const string LoginIp = "login-ip";
    public const string LoginEmail = "login-email";

    // SEC-RATE-02
    public const string Register = "register";

    // SEC-RATE-03
    public const string RecoveryIp = "recovery-ip";
    public const string RecoveryEmail = "recovery-email";

    // SEC-RATE-04
    public const string Refresh = "refresh";

    public static IReadOnlyList<string> All { get; } = [LoginIp, LoginEmail, Register, RecoveryIp, RecoveryEmail, Refresh];
}
