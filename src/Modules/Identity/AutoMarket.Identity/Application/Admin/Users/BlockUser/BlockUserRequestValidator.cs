using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Domain.Users;
using FluentValidation;

namespace AutoMarket.Identity.Application.Admin.Users.BlockUser;

// FR-ADM-01 AC1: səbəb məcburidir, 1–500 simvol
internal sealed class BlockUserRequestValidator : AbstractValidator<BlockUserRequest>
{
    public BlockUserRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithErrorCode(ValidationCodes.Required)
            .MaximumLength(User.BlockReasonMaxLength).WithErrorCode(ValidationCodes.TooLong);
    }
}
