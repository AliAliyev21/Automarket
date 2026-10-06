using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Domain.Tokens;

namespace AutoMarket.Identity.Application.Admin.Users.BlockUser;

// FR-ADM-01 AC1/AC2/AC5, R-04/R-05: bir transaksiyada status Blocked olur, bütün refresh tokenlər ləğv olunur, outbox-a
// UserBlocked (Listings/Notifications elanları gizlədir, bildirişləri dayandırır) və audit yazılır. Commit-dən sonra status
// cache-i silinir ki, mövcud access token dərhal rədd olunsun (AC4, SEC-AUTH-06). Təkrar bloklama idempotentdir
internal sealed class BlockUserHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUserStatusCache statusCache,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    TimeProvider time) : ICommandHandler<BlockUserCommand>
{
    public async Task<Result> HandleAsync(BlockUserCommand command, CancellationToken cancellationToken)
    {
        // R-05: Admin özünü bloklaya bilməz
        if (command.ActorId == command.UserId)
        {
            return CommonErrors.Forbidden;
        }

        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AdminErrors.UserNotFound;
        }

        var now = time.GetUtcNow();

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        if (!user.Block(command.Reason, now))
        {
            return Result.Success();
        }

        await refreshTokens.RevokeAllActiveAsync(user.Id, RefreshTokenRevocationReason.Blocked, now, cancellationToken);

        audit.Record(new AuditEntry(
            AdminAuditEvents.UserBlocked,
            AuthAuditEvents.ResultSuccess,
            ActorId: command.ActorId,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString(),
            Details: new Dictionary<string, string>(StringComparer.Ordinal) { [AdminAuditEvents.ReasonKey] = command.Reason }));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await statusCache.InvalidateAsync(user.Id, cancellationToken);
        return Result.Success();
    }
}
