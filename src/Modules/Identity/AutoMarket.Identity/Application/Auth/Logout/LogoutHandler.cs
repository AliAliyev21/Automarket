using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.BuildingBlocks.Security;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;

namespace AutoMarket.Identity.Application.Auth.Logout;

// FR-AUTH-05 AC1: cari sessiya (refresh token ailəsi) ləğv olunur. Cavab idempotentdir: cookie olmasa və ya token
// tanınmasa da uğurludur (endpoint cookie-ni hər halda silir)
internal sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokens,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    TimeProvider time) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(command.RefreshToken))
        {
            return Result.Success();
        }

        var token = await refreshTokens.FindByHashAsync(TokenHasher.Hash(command.RefreshToken), cancellationToken);
        if (token is null || !TokenHasher.Matches(command.RefreshToken, token.TokenHash))
        {
            return Result.Success();
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        await refreshTokens.RevokeFamilyAsync(token.FamilyId, RefreshTokenRevocationReason.Logout, time.GetUtcNow(), cancellationToken);

        audit.Record(new AuditEntry(
            AuthAuditEvents.Logout,
            AuthAuditEvents.ResultSuccess,
            ActorId: token.UserId,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: token.UserId.ToString(),
            Details: new Dictionary<string, string>(StringComparer.Ordinal) { [AuthAuditEvents.FamilyIdKey] = token.FamilyId.ToString() }));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
