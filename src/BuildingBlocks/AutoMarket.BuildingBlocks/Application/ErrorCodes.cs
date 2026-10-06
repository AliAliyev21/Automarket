namespace AutoMarket.BuildingBlocks.Application;

// Bütün modullar üçün ümumi xəta kodları (CONVENTIONS §5.4). Hər kod yalnız bir dəfə, yalnız REQUIREMENTS 4.8-dən
public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";
    public const string PayloadTooLarge = "PAYLOAD_TOO_LARGE";
    public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";

    // FR-AUTH-03 AC4, SEC-AUTH-06: həm login, həm status middleware (BuildingBlocks.Web) qaytarır
    public const string AccountBlocked = "ACCOUNT_BLOCKED";
}
