using Microsoft.Extensions.Options;

namespace AutoMarket.Notifications.Application;

internal sealed class NotificationsOptions
{
    public const string SectionName = "Notifications";

    public LinkSettings Links { get; set; } = new();

    internal sealed class LinkSettings
    {
        // Frontend səhifəsi; token query parametri kimi əlavə olunur. SEC-EXT-01: URL konfiqurasiyadadır, inputdan qurulmur
        public string ConfirmEmailUrl { get; set; } = string.Empty;

        // FR-AUTH-06
        public string ResetPasswordUrl { get; set; } = string.Empty;
    }
}

internal sealed class NotificationsOptionsValidator : IValidateOptions<NotificationsOptions>
{
    public ValidateOptionsResult Validate(string? name, NotificationsOptions options)
    {
        var failures = new List<string>();
        Check(options.Links.ConfirmEmailUrl, nameof(NotificationsOptions.LinkSettings.ConfirmEmailUrl), failures);
        Check(options.Links.ResetPasswordUrl, nameof(NotificationsOptions.LinkSettings.ResetPasswordUrl), failures);

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Check(string url, string key, List<string> failures)
    {
        if (!(Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.Query)))
        {
            failures.Add($"{NotificationsOptions.SectionName}:Links:{key} must be an absolute https:// URL without a query.");
        }
    }
}
