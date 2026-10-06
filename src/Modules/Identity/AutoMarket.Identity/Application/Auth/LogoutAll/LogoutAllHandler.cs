using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Domain.Tokens;

namespace AutoMarket.Identity.Application.Auth.LogoutAll;

// FR-AUTH-05 AC2: istifadəçinin bütün refresh tokenləri ləğv olunur. AC3: access token ömrünün sonuna qədər qalır (qəbul edilmiş risk)
internal sealed class LogoutAllHandler(
    IRefreshTokenRepository refreshTokens,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    TimeProvider time) : ICommandHandler<LogoutAllCommand>
{
    public async Task<Result> HandleAsync(LogoutAllCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        await refreshTokens.RevokeAllActiveAsync(command.UserId, RefreshTokenRevocationReason.LogoutAll, time.GetUtcNow(), cancellationToken);

        audit.Record(new AuditEntry(
            AuthAuditEvents.LogoutAll,
            AuthAuditEvents.ResultSuccess,
            ActorId: command.UserId,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: command.UserId.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
