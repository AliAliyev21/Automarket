namespace AutoMarket.BuildingBlocks.Security;

// SEC-SEC-03: nümunə konfiqurasiyadakı (.env.example, README) placeholder dəyərlə tətbiq işə düşmür
public static class SecretValue
{
    private static readonly string[] PlaceholderMarkers = ["change-me", "change_me", "changeme"];

    public static bool IsPlaceholder(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return PlaceholderMarkers.Any(marker => value.Contains(marker, StringComparison.OrdinalIgnoreCase))
            || (value.StartsWith('<') && value.EndsWith('>'));
    }
}
