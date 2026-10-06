using AutoMarket.BuildingBlocks.Application;
using AutoMarket.Identity.Application.Abstractions;

namespace AutoMarket.Identity.Application.Auth;

// SEC-AUTH-01: email/ad ilə müqayisə istifadəçi məlum olandan sonra (handler-də) aparılır. Nəticə validator xətası
// formatındadır: 400 VALIDATION_FAILED + errors { newPassword: [...] }
internal static class PasswordPolicyErrors
{
    public const string NewPasswordField = "newPassword";

    public static Error? Check(IPasswordPolicy policy, string password, string? email, string? name)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var violations = policy.Validate(password, email, name);
        return violations.Count == 0
            ? null
            : CommonErrors.ValidationFailedFor(
                NewPasswordField,
                violations.Select(violation => new FieldError(violation.Code, violation.Message)));
    }
}
