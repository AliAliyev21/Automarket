using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AutoMarket.Api.Configuration;

internal sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
}

internal sealed class RedisOptionsValidator : IValidateOptions<RedisOptions>
{
    private const string Key = $"{RedisOptions.SectionName}:{nameof(RedisOptions.ConnectionString)}";

    public ValidateOptionsResult Validate(string? name, RedisOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail($"{Key} is required.");
        }

        ConfigurationOptions parsed;
        try
        {
            parsed = ConfigurationOptions.Parse(options.ConnectionString);
        }
        catch (ArgumentException)
        {
            return ValidateOptionsResult.Fail($"{Key} is not a valid Redis connection string.");
        }

        if (parsed.EndPoints.Count == 0)
        {
            return ValidateOptionsResult.Fail($"{Key} must contain at least one endpoint.");
        }

        if (string.IsNullOrEmpty(parsed.Password) || SecretValue.IsPlaceholder(parsed.Password))
        {
            return ValidateOptionsResult.Fail($"{Key} must contain a non-placeholder password.");
        }

        return ValidateOptionsResult.Success;
    }
}
