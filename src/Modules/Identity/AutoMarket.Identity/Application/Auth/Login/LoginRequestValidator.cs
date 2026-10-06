using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.Register;
using FluentValidation;

namespace AutoMarket.Identity.Application.Auth.Login;

// Şifrə siyasəti login-də yoxlanılmır (köhnə şifrələr), yalnız uzunluğun yuxarı həddi hash hesablanmasını məhdudlaşdırır
internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public const int PasswordMaxLength = 1024;

    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(RegisterRequestValidator.EmailMaxLength).WithErrorCode(ValidationCodes.TooLong);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(PasswordMaxLength).WithErrorCode(ValidationCodes.TooLong);
    }
}
