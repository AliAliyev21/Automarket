using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;
using AutoMarket.Identity.Domain.Users;

namespace AutoMarket.Identity.Application.Auth.ResetPassword;

// FR-AUTH-06 AC2–AC4, SEC-AUTH-07: birdəfəlik token (hash ilə axtarış, sabit vaxtda müqayisə). Uğurlu reset-dən sonra
// bütün refresh tokenlər ləğv olunur, lockout sıfırlanır və "şifrəniz dəyişdirildi" məktubu göndərilir. Bloklanmış
// istifadəçinin tokeni qəbul edilmir. Hamısı bir transaksiyadadır
internal sealed class ResetPasswordHandler(
    IOneTimeTokenRepository tokens,
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordService passwords,
    IPasswordPolicy passwordPolicy,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    TimeProvider time) : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();

        var token = await tokens.FindByHashAsync(TokenHasher.Hash(command.Token), cancellationToken);
        if (token is null
            || token.Purpose != OneTimeTokenPurpose.PasswordReset
            || !TokenHasher.Matches(command.Token, token.TokenHash)
            || !token.IsUsable(now))
        {
            return AuthErrors.TokenInvalidOrExpired;
        }

        var user = await users.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null || user.Status == UserStatus.Blocked)
        {
            return AuthErrors.TokenInvalidOrExpired;
        }

        if (PasswordPolicyErrors.Check(passwordPolicy, command.NewPassword, user.Email, user.Name) is { } policyError)
        {
            return policyError;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        token.Use(now);
        passwords.SetPassword(user, command.NewPassword);
        user.CompletePasswordReset();
        await refreshTokens.RevokeAllActiveAsync(user.Id, RefreshTokenRevocationReason.PasswordReset, now, cancellationToken);

        audit.Record(new AuditEntry(
            AuthAuditEvents.PasswordResetCompleted,
            AuthAuditEvents.ResultSuccess,
            ActorId: user.Id,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
