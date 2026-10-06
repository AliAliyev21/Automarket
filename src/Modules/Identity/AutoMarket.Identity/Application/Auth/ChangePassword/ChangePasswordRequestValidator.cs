using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth.Login;
using FluentValidation;

namespace AutoMarket.Identity.Application.Auth.ChangePassword;

// FR-AUTH-07 AC1: cari şifrə tələb olunur (siyasət yoxlanılmır, yalnız yuxarı hədd), yeni şifrə SEC-AUTH-01-ə uymalıdır
internal sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator(IPasswordPolicy passwordPolicy)
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(LoginRequestValidator.PasswordMaxLength).WithErrorCode(ValidationCodes.TooLong);

        RuleFor(x => x.NewPassword).MustSatisfyPasswordPolicy(passwordPolicy);
    }
}
