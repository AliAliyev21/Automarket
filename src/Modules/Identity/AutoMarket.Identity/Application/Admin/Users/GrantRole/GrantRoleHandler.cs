using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;

namespace AutoMarket.Identity.Application.Admin.Users.GrantRole;

// FR-ADM-02 AC1–AC3: rol verilir, UserRolesChanged və audit yazılır. AC2: yeni rol növbəti refresh-də (≤ 15 dəq) access
// token-ə düşür, sessiyalar ləğv olunmur. Rol artıq varsa idempotentdir
internal sealed class GrantRoleHandler(
    IUserRepository users,
    IUserStatusCache statusCache,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit) : ICommandHandler<GrantRoleCommand>
{
    public async Task<Result> HandleAsync(GrantRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AdminErrors.UserNotFound;
        }

        var roles = await users.GetRolesAsync(user.Id, cancellationToken);
        if (!await users.AddRoleAsync(user.Id, command.Role, cancellationToken))
        {
            return Result.Success();
        }

        user.RecordRolesChanged([.. roles.Append(command.Role).Order(StringComparer.Ordinal)]);

        audit.Record(new AuditEntry(
            AdminAuditEvents.RoleGranted,
            AuthAuditEvents.ResultSuccess,
            ActorId: command.ActorId,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString(),
            Details: new Dictionary<string, string>(StringComparer.Ordinal) { [AdminAuditEvents.RoleKey] = command.Role }));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ARCHITECTURE §8.7: rol dəyişikliyi → user-status açarı silinir
        await statusCache.InvalidateAsync(user.Id, cancellationToken);
        return Result.Success();
    }
}
