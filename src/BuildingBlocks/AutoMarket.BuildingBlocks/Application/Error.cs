using System.Diagnostics.CodeAnalysis;

namespace AutoMarket.BuildingBlocks.Application;

// Gözlənilən xəta: sabit kod (REQUIREMENTS 4.8), ingiliscə mesaj və HTTP status (ARCHITECTURE §8.1)
[SuppressMessage("Naming", "CA1716", Justification = "Ad ARCHITECTURE §8.1 və CONVENTIONS §5.4-də sabitlənib; VB istehlakçısı yoxdur")]
public sealed record Error(string Code, string Message, int HttpStatus)
{
    // SEC-RATE: 429 cavabında Retry-After header-i üçün (yalnız RATE_LIMITED)
    public TimeSpan? RetryAfter { get; init; }

    // VALIDATION_FAILED: sahə → [kod, mesaj] (SEC-ERR-01). Validator-dan sonra handler-də aşkar olunan sahə xətaları üçün
    // (məs. yeni şifrənin istifadəçinin email-i/adı ilə müqayisəsi, SEC-AUTH-01)
    public IReadOnlyDictionary<string, IReadOnlyList<FieldError>>? FieldErrors { get; init; }
}

public sealed record FieldError(string Code, string Message);
