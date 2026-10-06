using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Application.Auth.Login;

// FR-AUTH-03. SEC-AUTH-08: hesab yoxdursa dummy hash yoxlanılır; EMAIL_NOT_CONFIRMED, ACCOUNT_BLOCKED və ACCOUNT_LOCKED_OUT yalnız şifrə
// düzgün olduqda qaytarılır. SEC-AUTH-03: kilid zamanı yanlış cəhd sayğacı artırmır (kilid uzanmır)
internal sealed class LoginHandler(
    IUserRepository users,
    IPasswordService passwords,
    SessionService sessions,
    IIdentityUnitOfWork unitOfWork,
    IRateLimitService rateLimits,
    IAuditLog audit,
    IOptions<IdentityOptions> options,
    TimeProvider time) : ICommandHandler<LoginCommand, SessionTokens>
{
    public async Task<Result<SessionTokens>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Normalize(command.Email);

        // SEC-RATE-01: hesab (email) üzrə limit, IP limiti middleware-dədir
        var decision = await rateLimits.TryAcquireAsync(RateLimitRules.LoginEmail, RateLimitKeys.ForEmail(email), cancellationToken);
        if (!decision.IsAllowed)
        {
            return CommonErrors.RateLimitedFor(decision.RetryAfter);
        }

        var user = await users.FindByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            passwords.SimulateVerification(command.Password);
            return await FailAsync(null, AuthAuditEvents.ReasonUnknownAccount, AuthErrors.InvalidCredentials, cancellationToken);
        }

        var now = time.GetUtcNow();
        var check = passwords.Verify(user, command.Password);

        if (check == PasswordCheckResult.Failed)
        {
            if (!user.IsLockedOut(now) && user.RecordFailedLogin(now, options.Value.Lockout.ToPolicy()))
            {
                audit.Record(new AuditEntry(
                    AuthAuditEvents.LockedOut,
                    AuthAuditEvents.ResultSuccess,
                    TargetType: AuthAuditEvents.TargetUser,
                    TargetId: user.Id.ToString()));
            }

            return await FailAsync(user, AuthAuditEvents.ReasonInvalidPassword, AuthErrors.InvalidCredentials, cancellationToken);
        }

        if (check == PasswordCheckResult.SuccessRehashNeeded)
        {
            passwords.Rehash(user, command.Password);
        }

        if (user.IsLockedOut(now))
        {
            return await FailAsync(user, AuthAuditEvents.ReasonLockedOut, AuthErrors.AccountLockedOut, cancellationToken);
        }

        // FR-AUTH-03 AC4, R-04: yalnız şifrə düzgün olduqda
        if (user.Status == UserStatus.Blocked)
        {
            return await FailAsync(user, AuthAuditEvents.ReasonBlocked, CommonErrors.AccountBlocked, cancellationToken);
        }

        if (user.Status == UserStatus.Unconfirmed)
        {
            return await FailAsync(user, AuthAuditEvents.ReasonEmailNotConfirmed, AuthErrors.EmailNotConfirmed, cancellationToken);
        }

        user.RecordSuccessfulLogin();
        var tokens = await sessions.StartAsync(user.Id, now, cancellationToken);

        audit.Record(new AuditEntry(
            AuthAuditEvents.LoginSucceeded,
            AuthAuditEvents.ResultSuccess,
            ActorId: user.Id,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return tokens;
    }

    // FR-AUTH-03 AC6: uğursuz cəhd audit olunur; istifadəçi dəyişiklikləri (sayğac, lockout) eyni transaksiyada yazılır
    private async Task<Result<SessionTokens>> FailAsync(User? user, string reason, Error error, CancellationToken cancellationToken)
    {
        audit.Record(new AuditEntry(
            AuthAuditEvents.LoginFailed,
            AuthAuditEvents.ResultFailure,
            TargetType: user is null ? null : AuthAuditEvents.TargetUser,
            TargetId: user?.Id.ToString(),
            Details: AuthAuditEvents.Reason(reason)));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return error;
    }
}
