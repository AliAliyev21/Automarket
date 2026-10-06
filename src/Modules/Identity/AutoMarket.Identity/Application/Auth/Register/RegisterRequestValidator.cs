using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Abstractions;
using FluentValidation;

namespace AutoMarket.Identity.Application.Auth.Register;

// FR-AUTH-01 AC1/AC3. Format və diapazon yoxlanılır; email-in mövcudluğu burada yoxlanılmır (SEC-AUTH-08)
internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int EmailMaxLength = 254;
    public const int NameMinLength = 2;
    public const int NameMaxLength = 50;

    public RegisterRequestValidator(IPasswordPolicy passwordPolicy)
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(EmailMaxLength).WithErrorCode(ValidationCodes.TooLong)
            .EmailAddress().WithErrorCode(ValidationCodes.InvalidFormat);

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MinimumLength(NameMinLength).WithErrorCode(ValidationCodes.TooShort)
            .MaximumLength(NameMaxLength).WithErrorCode(ValidationCodes.TooLong);

        // FR-AUTH-01 AC1, Q20: yalnız +994XXXXXXXXX
        RuleFor(x => x.Phone)
            .Matches(PhoneNumberFormat.Pattern).WithErrorCode(ValidationCodes.InvalidFormat)
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.TermsAccepted)
            .NotNull().WithErrorCode(ValidationCodes.Required)
            .Equal(true).WithErrorCode(ValidationCodes.MustBeTrue);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .Custom((password, context) =>
            {
                if (string.IsNullOrEmpty(password))
                {
                    return;
                }

                var request = context.InstanceToValidate;
                foreach (var violation in passwordPolicy.Validate(password, request.Email, request.Name))
                {
                    context.AddFailure(new FluentValidation.Results.ValidationFailure(context.PropertyPath, violation.Message)
                    {
                        ErrorCode = violation.Code,
                    });
                }
            });
    }
}
