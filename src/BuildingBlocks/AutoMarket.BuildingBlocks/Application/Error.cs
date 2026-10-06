using System.Diagnostics.CodeAnalysis;

namespace AutoMarket.BuildingBlocks.Application;

// Gözlənilən xəta: sabit kod (REQUIREMENTS 4.8), ingiliscə mesaj və HTTP status (ARCHITECTURE §8.1)
[SuppressMessage("Naming", "CA1716", Justification = "Ad ARCHITECTURE §8.1 və CONVENTIONS §5.4-də sabitlənib; VB istehlakçısı yoxdur")]
public sealed record Error(string Code, string Message, int HttpStatus)
{
    // SEC-RATE: 429 cavabında Retry-After header-i üçün (yalnız RATE_LIMITED)
    public TimeSpan? RetryAfter { get; init; }
}
