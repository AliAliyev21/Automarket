using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.Register;
using FluentValidation;

namespace AutoMarket.Identity.Application.Auth.ResendConfirmation;

internal sealed class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
{
    public ResendConfirmationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(RegisterRequestValidator.EmailMaxLength).WithErrorCode(ValidationCodes.TooLong)
            .EmailAddress().WithErrorCode(ValidationCodes.InvalidFormat);
    }
}
