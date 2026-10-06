namespace AutoMarket.BuildingBlocks.Application;

// Ümumi kodların standart mesajı və statusu. Mesajda daxili detal olmur (SEC-ERR-02)
public static class CommonErrors
{
    public static readonly Error ValidationFailed =
        new(ErrorCodes.ValidationFailed, "One or more fields are invalid.", 400);

    public static readonly Error Unauthorized =
        new(ErrorCodes.Unauthorized, "Authentication is required.", 401);

    public static readonly Error Forbidden =
        new(ErrorCodes.Forbidden, "Access is denied.", 403);

    public static readonly Error ConcurrencyConflict =
        new(ErrorCodes.ConcurrencyConflict, "The resource was modified by another request.", 409);

    public static readonly Error RateLimited =
        new(ErrorCodes.RateLimited, "Too many requests.", 429);

    public static readonly Error InternalError =
        new(ErrorCodes.InternalError, "An unexpected error occurred.", 500);

    public static readonly Error PayloadTooLarge =
        new(ErrorCodes.PayloadTooLarge, "Request body is too large.", 413);

    public static readonly Error UnsupportedMediaType =
        new(ErrorCodes.UnsupportedMediaType, "Unsupported content type.", 415);

    public static Error RateLimitedFor(TimeSpan retryAfter) => RateLimited with { RetryAfter = retryAfter };
}
