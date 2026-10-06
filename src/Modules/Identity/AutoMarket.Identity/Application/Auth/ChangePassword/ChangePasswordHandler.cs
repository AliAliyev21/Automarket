using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;
using Microsoft.Extensions.Options;

namespace AutoMarket.Identity.Application.Auth.ChangePassword;

// FR-AUTH-07: cari şifrə yoxlanılır; uğurlu dəyişiklikdən sonra cari sessiyadan (refresh cookie-nin ailəsi) başqa bütün
// refresh tokenlər ləğv olunur və bildiriş məktubu göndərilir. Cookie yoxdursa və ya başqa istifadəçiyə aiddirsə bütün
// sessiyalar ləğv olunur. Brute-force qarşısı: yanlış cari şifrə login ilə eyni lockout sayğacına yazılır (SEC-AUTH-03)
internal sealed class ChangePasswordHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordService passwords,
    IPasswordPolicy passwordPolicy,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    IOptions<IdentityOptions> options,
    TimeProvider time) : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();

        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is not { Status: UserStatus.Active })
        {
            return CommonErrors.Unauthorized;
        }

        // Kilid müddətində şifrə yoxlanılmır (cəhdlər kilidi uzatmır və təxmin etməyə imkan vermir)
        if (user.IsLockedOut(now))
        {
            return AuthErrors.AccountLockedOut;
        }

        if (passwords.Verify(user, command.CurrentPassword) == PasswordCheckResult.Failed)
        {
            return await FailAsync(user, now, cancellationToken);
        }

        if (PasswordPolicyErrors.Check(passwordPolicy, command.NewPassword, user.Email, user.Name) is { } policyError)
        {
            return policyError;
        }

        var currentFamilyId = await FindCurrentFamilyAsync(command, now, cancellationToken);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        passwords.SetPassword(user, command.NewPassword);
        user.RecordPasswordChange();

        if (currentFamilyId is { } familyId)
        {
            await refreshTokens.RevokeAllActiveExceptFamilyAsync(user.Id, familyId, RefreshTokenRevocationReason.PasswordChanged, now, cancellationToken);
        }
        else
        {
            await refreshTokens.RevokeAllActiveAsync(user.Id, RefreshTokenRevocationReason.PasswordChanged, now, cancellationToken);
        }

        audit.Record(new AuditEntry(
            AuthAuditEvents.PasswordChanged,
            AuthAuditEvents.ResultSuccess,
            ActorId: user.Id,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> FailAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (user.RecordFailedLogin(now, options.Value.Lockout.ToPolicy()))
        {
            audit.Record(new AuditEntry(
                AuthAuditEvents.LockedOut,
                AuthAuditEvents.ResultSuccess,
                TargetType: AuthAuditEvents.TargetUser,
                TargetId: user.Id.ToString()));
        }

        audit.Record(new AuditEntry(
            AuthAuditEvents.PasswordChangeFailed,
            AuthAuditEvents.ResultFailure,
            ActorId: user.Id,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString(),
            Details: AuthAuditEvents.Reason(AuthAuditEvents.ReasonInvalidPassword)));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AuthErrors.InvalidCredentials;
    }

    // Cari sessiya yalnız cookie-dəki token aktivdirsə və bu istifadəçiyə aiddirsə saxlanılır
    private async Task<Guid?> FindCurrentFamilyAsync(ChangePasswordCommand command, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return null;
        }

        var token = await refreshTokens.FindByHashAsync(TokenHasher.Hash(command.RefreshToken), cancellationToken);
        return token is not null
            && TokenHasher.Matches(command.RefreshToken, token.TokenHash)
            && token.UserId == command.UserId
            && token.IsActive(now)
                ? token.FamilyId
                : null;
    }
}
