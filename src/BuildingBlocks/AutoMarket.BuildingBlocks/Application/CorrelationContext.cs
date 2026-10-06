namespace AutoMarket.BuildingBlocks.Application;

// Correlation id HTTP middleware və consumer host tərəfindən təyin olunur, outbox envelope-una buradan yazılır (NFR-CORR)
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> CurrentValue = new();

    public static string? Current
    {
        get => CurrentValue.Value;
        set => CurrentValue.Value = value;
    }
}
