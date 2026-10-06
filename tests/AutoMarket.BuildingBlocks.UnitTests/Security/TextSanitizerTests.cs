using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.BuildingBlocks.UnitTests.Security;

// SEC-INP-05
public sealed class TextSanitizerTests
{
    [Fact]
    public void Sanitize_Null_ReturnsNull() => TextSanitizer.Sanitize(null).ShouldBeNull();

    [Fact]
    public void Sanitize_SurroundingWhitespace_Trimmed() => TextSanitizer.Sanitize("  Leyla  ").ShouldBe("Leyla");

    [Fact]
    public void Sanitize_ControlAndFormatCharacters_Removed() =>
        TextSanitizer.Sanitize("Ley\u0000la​‮\u0007").ShouldBe("Leyla");

    [Fact]
    public void Sanitize_NewLineAndTab_Kept() => TextSanitizer.Sanitize("a\nb\tc").ShouldBe("a\nb\tc");

    [Fact]
    public void Sanitize_DecomposedAzerbaijaniLetters_NormalizedToNfc()
    {
        // "ş" = s + U+0327, "ü" = u + U+0308
        var decomposed = "şü";

        TextSanitizer.Sanitize(decomposed).ShouldBe("şü");
    }

    [Fact]
    public void Sanitize_Emoji_Kept() => TextSanitizer.Sanitize("🚗 maşın").ShouldBe("🚗 maşın");
}
