using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Application.Auth.ForgotPassword;

// FR-AUTH-06 AC1/AC2: cavab email-in mövcudluğundan asılı deyil (SEC-AUTH-08). Token 256 bit, hash ilə saxlanılır, 1 saat
// etibarlıdır, yeni sorğu əvvəlkini etibarsız edir. Bloklanmış hesaba məktub getmir (cavab yenə eynidir).
// SEC-RATE-03: email limiti (resend-confirmation ilə ortaq) handler-də, IP limiti middleware-dədir
internal sealed class ForgotPasswordHandler(
    IUserRepository users,
    IOneTimeTokenRepository tokens,
    ISecureTokenGenerator tokenGenerator,
    IIdentityUnitOfWork unitOfWork,
    IRateLimitService rateLimits,
    IAuditLog audit,
    IIdGenerator ids,
    IOptions<IdentityOptions> options,
    TimeProvider time) : ICommandHandler<ForgotPasswordCommand>
{
    public async Task<Result> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Normalize(command.Email);

        var decision = await rateLimits.TryAcquireAsync(RateLimitRules.RecoveryEmail, RateLimitKeys.ForEmail(email), cancellationToken);
        if (!decision.IsAllowed)
        {
            return CommonErrors.RateLimitedFor(decision.RetryAfter);
        }

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null || user.Status == UserStatus.Blocked)
        {
            return Result.Success();
        }

        var now = time.GetUtcNow();
        await tokens.RevokeActiveAsync(user.Id, OneTimeTokenPurpose.PasswordReset, now, cancellationToken);

        var rawToken = tokenGenerator.Generate();
        var token = OneTimeToken.Issue(
            ids.NewId(),
            user.Id,
            OneTimeTokenPurpose.PasswordReset,
            TokenHasher.Hash(rawToken),
            now,
            now + options.Value.Tokens.PasswordResetLifetime);
        tokens.Add(token);

        user.RequestPasswordReset(rawToken, token.ExpiresAt);

        audit.Record(new AuditEntry(
            AuthAuditEvents.PasswordResetRequested,
            AuthAuditEvents.ResultSuccess,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
