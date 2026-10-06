using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.Register;
using FluentValidation;

namespace AutoMarket.Identity.Application.Auth.ForgotPassword;

internal sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(RegisterRequestValidator.EmailMaxLength).WithErrorCode(ValidationCodes.TooLong)
            .EmailAddress().WithErrorCode(ValidationCodes.InvalidFormat);
    }
}
