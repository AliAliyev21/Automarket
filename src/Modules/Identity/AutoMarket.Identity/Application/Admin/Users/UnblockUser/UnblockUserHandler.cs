using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Audit;
using AutoMarket.Identity.Application.Abstractions;
using AutoMarket.Identity.Application.Auth;

namespace AutoMarket.Identity.Application.Admin.Users.UnblockUser;

// FR-ADM-01 AC1/AC3/AC5: status bloklanmadan əvvəlki vəziyyətə qayıdır, outbox-a UserUnblocked (Listings elanları yenidən
// göstərir) və audit yazılır. Ləğv olunmuş sessiyalar bərpa olunmur: istifadəçi yenidən login edir. Bloklanmamış istifadəçi üçün idempotentdir
internal sealed class UnblockUserHandler(
    IUserRepository users,
    IUserStatusCache statusCache,
    IIdentityUnitOfWork unitOfWork,
    IAuditLog audit) : ICommandHandler<UnblockUserCommand>
{
    public async Task<Result> HandleAsync(UnblockUserCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return AdminErrors.UserNotFound;
        }

        if (!user.Unblock())
        {
            return Result.Success();
        }

        audit.Record(new AuditEntry(
            AdminAuditEvents.UserUnblocked,
            AuthAuditEvents.ResultSuccess,
            ActorId: command.ActorId,
            TargetType: AuthAuditEvents.TargetUser,
            TargetId: user.Id.ToString()));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await statusCache.InvalidateAsync(user.Id, cancellationToken);
        return Result.Success();
    }
}
