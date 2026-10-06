namespace AutoMarket.Identity.Application.Admin.Users.GrantRole;

// FR-ADM-02 AC1: rol təyin etmək bu endpoint-in məqsədidir (yalnız Admin policy-si ilə). Rol allow-list ilə yoxlanılır
// (AssignableRoles), ona görə SEC-INP-03 qaydasında açıq istisnadır (ConventionTests)
internal sealed record GrantRoleRequest(string? Role)
{
    public GrantRoleCommand ToCommand(Guid actorId, Guid userId) => new(actorId, userId, AssignableRoles.Normalize(Role)!);
}
