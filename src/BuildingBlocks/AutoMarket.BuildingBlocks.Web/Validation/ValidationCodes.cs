namespace AutoMarket.BuildingBlocks.Web.Validation;

// Validasiya sahə kodları: UPPER_SNAKE_CASE, hər qaydanın sabit kodu var (ARCHITECTURE §8.2, CONVENTIONS §2.5)
public static class ValidationCodes
{
    public const string Required = "REQUIRED";
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string TooShort = "TOO_SHORT";
    public const string TooLong = "TOO_LONG";
    public const string OutOfRange = "OUT_OF_RANGE";
    public const string MustBeTrue = "MUST_BE_TRUE";
    public const string TooManyItems = "TOO_MANY_ITEMS";
    public const string PasswordTooShort = "PASSWORD_TOO_SHORT";
    public const string PasswordTooLong = "PASSWORD_TOO_LONG";
    public const string PasswordTooCommon = "PASSWORD_TOO_COMMON";
    public const string PasswordContainsPersonalInfo = "PASSWORD_CONTAINS_PERSONAL_INFO";
}
