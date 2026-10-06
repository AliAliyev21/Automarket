namespace AutoMarket.BuildingBlocks.Domain;

// Kod yalnız REQUIREMENTS 4.8 siyahısından götürülür (CONVENTIONS §4.4)
public sealed record DomainError(string Code, string Message, DomainErrorKind Kind)
{
    public static DomainError Conflict(string code, string message) => new(code, message, DomainErrorKind.Conflict);

    public static DomainError Validation(string code, string message) => new(code, message, DomainErrorKind.Validation);
}
