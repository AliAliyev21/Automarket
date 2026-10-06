using AutoMarket.BuildingBlocks.Web.Validation;
using FluentValidation;

namespace AutoMarket.Identity.Application.Admin.Users.GrantRole;

// SEC-INP-01: yalnız Moderator və ya Admin
internal sealed class GrantRoleRequestValidator : AbstractValidator<GrantRoleRequest>
{
    public GrantRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .Must(role => AssignableRoles.Normalize(role) is not null).WithErrorCode(ValidationCodes.InvalidFormat)
            .WithMessage($"Role must be one of: {string.Join(", ", AssignableRoles.All)}.");
    }
}
