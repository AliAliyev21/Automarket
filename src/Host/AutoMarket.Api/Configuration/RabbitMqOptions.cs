using Microsoft.Extensions.Options;

namespace AutoMarket.Api.Configuration;

internal sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    // amqp://user:password@host:5672/vhost
    public string ConnectionString { get; set; } = string.Empty;
}

internal sealed class RabbitMqOptionsValidator : IValidateOptions<RabbitMqOptions>
{
    private const string Key = $"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.ConnectionString)}";

    public ValidateOptionsResult Validate(string? name, RabbitMqOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail($"{Key} is required.");
        }

        if (!Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out var uri) || uri.Scheme is not ("amqp" or "amqps"))
        {
            return ValidateOptionsResult.Fail($"{Key} must be an absolute amqp:// or amqps:// URI.");
        }

        var separator = uri.UserInfo.IndexOf(':', StringComparison.Ordinal);
        var password = separator < 0 ? string.Empty : Uri.UnescapeDataString(uri.UserInfo[(separator + 1)..]);
        if (separator <= 0 || string.IsNullOrEmpty(password) || SecretValue.IsPlaceholder(password))
        {
            return ValidateOptionsResult.Fail($"{Key} must contain a user name and a non-placeholder password.");
        }

        return ValidateOptionsResult.Success;
    }
}
