using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;

namespace AutoMarket.Identity.Application.Auth.ConfirmEmail;

// FR-AUTH-02 AC2: etibarlı token hesabı təsdiqləyir; istifadə olunmuş, vaxtı keçmiş və ya yanlış token → TOKEN_INVALID_OR_EXPIRED.
// SEC-AUTH-07: axtarış hash ilə aparılır, müqayisə sabit vaxtdadır
internal sealed class ConfirmEmailHandler(
    IOneTimeTokenRepository tokens,
    IUserRepository users,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    TimeProvider time) : ICommandHandler<ConfirmEmailCommand>
{
    public async Task<Result> HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();

        var token = await tokens.FindByHashAsync(TokenHasher.Hash(command.Token), cancellationToken);
        if (token is null
            || token.Purpose != OneTimeTokenPurpose.EmailConfirmation
            || !TokenHasher.Matches(command.Token, token.TokenHash)
            || !token.IsUsable(now))
        {
            return AuthErrors.TokenInvalidOrExpired;
        }

        var user = await users.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            return AuthErrors.TokenInvalidOrExpired;
        }

        token.Use(now);
        user.ConfirmEmail(now);

        audit.Record(new AuditEntry(
            AuthAuditEvents.EmailConfirmed,
            AuthAuditEvents.ResultSuccess,
            ActorId: user.Id,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
