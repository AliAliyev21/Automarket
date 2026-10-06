using AutoMarket.BuildingBlocks.Security;
using MailKit.Security;
using Microsoft.Extensions.Options;

namespace AutoMarket.Notifications.Infrastructure.Email;

// NFR-ENV-01/02: lokal Mailpit (Security = None), digər mühitlərdə real SMTP + StartTls. Parol yalnız user-secrets/env ilə
internal sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = string.Empty;

    // SEC-EXT-01: xarici çağırışlarda timeout
    public TimeSpan Timeout { get; set; }
}

internal sealed class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
    private const string Section = SmtpOptions.SectionName;

    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host) || options.Port is <= 0 or > 65535)
        {
            failures.Add($"{Section}:Host and {Section}:Port are required.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress) || !options.FromAddress.Contains('@', StringComparison.Ordinal))
        {
            failures.Add($"{Section}:FromAddress must be an email address.");
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            failures.Add($"{Section}:Timeout must be positive.");
        }

        if (!string.IsNullOrEmpty(options.Password) && SecretValue.IsPlaceholder(options.Password))
        {
            failures.Add($"{Section}:Password must not be a placeholder.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
