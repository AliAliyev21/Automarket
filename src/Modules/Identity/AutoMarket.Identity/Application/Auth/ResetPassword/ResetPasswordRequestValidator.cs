using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth.ConfirmEmail;
using FluentValidation;

namespace AutoMarket.Identity.Application.Auth.ResetPassword;

// FR-AUTH-06 AC3: yeni şifrə SEC-AUTH-01 siyasətinə uymalıdır
internal sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator(IPasswordPolicy passwordPolicy)
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(ConfirmEmailRequestValidator.TokenMaxLength).WithErrorCode(ValidationCodes.TooLong);

        RuleFor(x => x.NewPassword).MustSatisfyPasswordPolicy(passwordPolicy);
    }
}
