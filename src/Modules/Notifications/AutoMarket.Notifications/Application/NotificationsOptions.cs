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
    }
}

internal sealed class NotificationsOptionsValidator : IValidateOptions<NotificationsOptions>
{
    public ValidateOptionsResult Validate(string? name, NotificationsOptions options)
    {
        var url = options.Links.ConfirmEmailUrl;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(uri.Query)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"{NotificationsOptions.SectionName}:Links:ConfirmEmailUrl must be an absolute https:// URL without a query.");
    }
}
