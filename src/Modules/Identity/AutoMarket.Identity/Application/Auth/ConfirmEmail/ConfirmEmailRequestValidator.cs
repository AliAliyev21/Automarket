using AutoMarket.BuildingBlocks.Web.Validation;
using FluentValidation;

namespace AutoMarket.Identity.Application.Auth.ConfirmEmail;

internal sealed class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    // 256 bit token base64url-də 43 simvoldur; yuxarı hədd hash hesablanmasını məhdudlaşdırır
    public const int TokenMaxLength = 128;

    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(TokenMaxLength).WithErrorCode(ValidationCodes.TooLong);
    }
}
