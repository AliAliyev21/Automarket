using AutoMarket.BuildingBlocks.Application;
using Microsoft.Extensions.Options;

namespace AutoMarket.BuildingBlocks.Web.RateLimiting;

// Bütün limitlər konfiqurasiyadan oxunur (REQUIREMENTS 4.5, ARCHITECTURE §7.4)
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    // Redis açarlarının prefiksi; mühitlər (və testlər) eyni Redis-i paylaşanda açarlar qarışmır
    public string KeyPrefix { get; set; } = "rl";

    public IDictionary<string, RateLimitRule> Rules { get; } = new Dictionary<string, RateLimitRule>(StringComparer.Ordinal);

    // ADR-0003: şifrə hash-i CPU-nu yükləyir, login üçün eyni anda işlənən sorğular məhdudlaşdırılır. 0 → CPU sayı × 2
    public ConcurrencySettings LoginConcurrency { get; set; } = new();

    public RateLimitRule GetRule(string name) =>
        Rules.TryGetValue(name, out var rule)
            ? rule
            : throw new InvalidOperationException($"Rate limit rule '{name}' is not configured.");

    public sealed class ConcurrencySettings
    {
        public int PermitLimit { get; set; }

        public int QueueLimit { get; set; } = 100;

        public int EffectivePermitLimit => PermitLimit > 0 ? PermitLimit : Environment.ProcessorCount * 2;
    }
}

public sealed class RateLimitRule
{
    public int PermitLimit { get; set; }

    public TimeSpan Window { get; set; }
}

internal sealed class RateLimitingOptionsValidator : IValidateOptions<RateLimitingOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitingOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.KeyPrefix))
        {
            failures.Add($"{RateLimitingOptions.SectionName}:KeyPrefix is required.");
        }

        foreach (var rule in RateLimitRules.All.Where(rule => !options.Rules.ContainsKey(rule)))
        {
            failures.Add($"{RateLimitingOptions.SectionName}:Rules:{rule} is required.");
        }

        foreach (var (ruleName, rule) in options.Rules)
        {
            if (rule.PermitLimit <= 0 || rule.Window <= TimeSpan.Zero)
            {
                failures.Add($"{RateLimitingOptions.SectionName}:Rules:{ruleName} must have a positive PermitLimit and Window.");
            }
        }

        if (options.LoginConcurrency.PermitLimit < 0 || options.LoginConcurrency.QueueLimit < 0)
        {
            failures.Add($"{RateLimitingOptions.SectionName}:LoginConcurrency limits must not be negative.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
