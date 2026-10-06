using Microsoft.Extensions.Options;

namespace AutoMarket.BuildingBlocks.Web.Security;

// SEC-NET-03: icazə verilən origin-lər mühitə görə konfiqurasiyadan oxunur. Wildcard yoxdur, yalnız https://
public sealed class AllowedOriginsOptions
{
    public const string SectionName = "Cors";

    public IList<string> AllowedOrigins { get; } = [];

    public bool IsAllowed(string? origin) =>
        !string.IsNullOrEmpty(origin) && AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
}

internal sealed class AllowedOriginsOptionsValidator : IValidateOptions<AllowedOriginsOptions>
{
    private const string Key = $"{AllowedOriginsOptions.SectionName}:{nameof(AllowedOriginsOptions.AllowedOrigins)}";

    public ValidateOptionsResult Validate(string? name, AllowedOriginsOptions options)
    {
        foreach (var origin in options.AllowedOrigins)
        {
            if (origin.Contains('*', StringComparison.Ordinal))
            {
                return ValidateOptionsResult.Fail($"{Key} must not contain wildcards.");
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || uri.Scheme != Uri.UriSchemeHttps
                || uri.AbsolutePath != "/"
                || origin.EndsWith('/'))
            {
                return ValidateOptionsResult.Fail($"{Key} entries must be https:// origins without a path.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
