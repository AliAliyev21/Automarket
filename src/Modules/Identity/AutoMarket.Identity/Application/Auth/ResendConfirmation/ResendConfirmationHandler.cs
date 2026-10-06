using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Users;

namespace AutoMarket.Identity.Application.Auth.ResendConfirmation;

// FR-AUTH-02 AC3: cavab email-in mövcudluğundan asılı deyil (SEC-AUTH-08); yeni token köhnəsini etibarsız edir.
// SEC-RATE-03: email üzrə limit handler-də, IP üzrə limit middleware-dədir
internal sealed class ResendConfirmationHandler(
    IUserRepository users,
    EmailConfirmationIssuer emailConfirmation,
    IIdentityUnitOfWork unitOfWork,
    IRateLimitService rateLimits,
    TimeProvider time) : ICommandHandler<ResendConfirmationCommand>
{
    public async Task<Result> HandleAsync(ResendConfirmationCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Normalize(command.Email);

        var decision = await rateLimits.TryAcquireAsync(RateLimitRules.RecoveryEmail, RateLimitKeys.ForEmail(email), cancellationToken);
        if (!decision.IsAllowed)
        {
            return CommonErrors.RateLimitedFor(decision.RetryAfter);
        }

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is { Status: UserStatus.Unconfirmed })
        {
            await emailConfirmation.IssueAsync(user, time.GetUtcNow(), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
