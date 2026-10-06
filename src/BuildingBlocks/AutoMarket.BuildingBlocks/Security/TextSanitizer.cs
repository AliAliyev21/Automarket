using System.Globalization;
using System.Text;

namespace AutoMarket.BuildingBlocks.Security;

// SEC-INP-05: NFC normallaşdırma, \n və \t-dən başqa idarəedici və format simvolları silinir, kənar boşluqlar silinir
public static class TextSanitizer
{
    public static string? Sanitize(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var normalized = value.Normalize(NormalizationForm.FormC);
        var builder = new StringBuilder(normalized.Length);

        foreach (var rune in normalized.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            var isAllowedControl = rune.Value is '\n' or '\t';
            if (!isAllowedControl && category is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                continue;
            }

            builder.Append(rune.ToString());
        }

        return builder.ToString().Trim();
    }
}
