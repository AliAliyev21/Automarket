using AutoMarket.BuildingBlocks.Security;
using AutoMarket.BuildingBlocks.Web.Validation;

namespace AutoMarket.Identity.Application.Admin.Users.BlockUser;

// FR-ADM-01 AC1. Hədəf istifadəçi route-dan, icraçı tokendən gəlir (SEC-INP-03)
internal sealed record BlockUserRequest(string? Reason) : ISanitizableRequest<BlockUserRequest>
{
    // SEC-INP-05
    public BlockUserRequest Sanitize() => this with { Reason = TextSanitizer.Sanitize(Reason) };

    public BlockUserCommand ToCommand(Guid actorId, Guid userId) => new(actorId, userId, Reason!);
}
