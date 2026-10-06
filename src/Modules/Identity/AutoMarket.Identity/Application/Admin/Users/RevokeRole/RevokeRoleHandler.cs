using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;
using AutoMarket.Identity.Domain.Tokens;

namespace AutoMarket.Identity.Application.Admin.Users.RevokeRole;

// FR-ADM-02 AC1–AC3: rol azaldılanda bütün refresh tokenlər ləğv olunur (köhnə rolla yeni access token alınmasın), status
// cache-i silinir, UserRolesChanged və audit yazılır. R-05: Admin öz Admin rolunu ləğv edə bilməz (bu, həm də sistemdə ən
// azı bir aktiv Admin qalmasını təmin edir: icraçı özü aktiv Admin-dir). Rol yoxdursa idempotentdir
internal sealed class RevokeRoleHandler(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUserStatusCache statusCache,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit,
    TimeProvider time) : ICommandHandler<RevokeRoleCommand>
{
    public async Task<Result> HandleAsync(RevokeRoleCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorId == command.UserId && command.Role == Roles.Admin)
        {
            return CommonErrors.Forbidden;
        }

        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AdminErrors.UserNotFound;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var roles = await users.GetRolesAsync(user.Id, cancellationToken);
        if (!await users.RemoveRoleAsync(user.Id, command.Role, cancellationToken))
        {
            return Result.Success();
        }

        user.RecordRolesChanged([.. roles.Where(role => role != command.Role)]);
        await refreshTokens.RevokeAllActiveAsync(user.Id, RefreshTokenRevocationReason.RoleDowngraded, time.GetUtcNow(), cancellationToken);

        audit.Record(new AuditEntry(
            AdminAuditEvents.RoleRevoked,
            AuthAuditEvents.ResultSuccess,
            ActorId: command.ActorId,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString(),
            Details: new Dictionary<string, string>(StringComparer.Ordinal) { [AdminAuditEvents.RoleKey] = command.Role }));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await statusCache.InvalidateAsync(user.Id, cancellationToken);
        return Result.Success();
    }
}
