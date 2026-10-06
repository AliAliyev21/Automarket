namespace AutoMarket.Api.Configuration;

// SEC-SEC-03: nümunə konfiqurasiyadakı (.env.example, README) placeholder dəyərlə tətbiq işə düşmür
internal static class SecretValue
{
    private static readonly string[] PlaceholderMarkers = ["change-me", "change_me", "changeme"];

    public static bool IsPlaceholder(string value) =>
        PlaceholderMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
        || (value.StartsWith('<') && value.EndsWith('>'));
}
