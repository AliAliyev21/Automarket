using Microsoft.Extensions.Primitives;

namespace AutoMarket.BuildingBlocks.Web.Correlation;

// Correlation id formatı: ≤ 64 simvol, [A-Za-z0-9-_] (REQUIREMENTS NFR-CORR)
public static class CorrelationIdFormat
{
    public const string HeaderName = "X-Correlation-Id";

    internal const string ItemKey = "AutoMarket.CorrelationId";

    private const int MaxLength = 64;

    public static bool IsValid(StringValues values)
    {
        if (values.Count != 1)
        {
            return false;
        }

        var value = values[0];
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength)
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')
            {
                return false;
            }
        }

        return true;
    }
}
