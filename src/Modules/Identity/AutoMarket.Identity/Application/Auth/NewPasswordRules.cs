using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Abstractions;
using FluentValidation;
using FluentValidation.Results;

namespace AutoMarket.Identity.Application.Auth;

// SEC-AUTH-01: uzunluq və sızmış şifrələr siyahısı validator-da yoxlanılır. Email/ad ilə müqayisə istifadəçi məlum
// olandan sonra handler-də aparılır (PasswordPolicyErrors)
internal static class NewPasswordRules
{
    public static IRuleBuilderOptionsConditions<T, string?> MustSatisfyPasswordPolicy<T>(
        this IRuleBuilder<T, string?> rule,
        IPasswordPolicy passwordPolicy) =>
        rule
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .Custom((password, context) =>
            {
                if (string.IsNullOrEmpty(password))
                {
                    return;
                }

                foreach (var violation in passwordPolicy.Validate(password, email: null, name: null))
                {
                    context.AddFailure(new ValidationFailure(context.PropertyPath, violation.Message) { ErrorCode = violation.Code });
                }
            });
}
