using AutoMarket.BuildingBlocks.Application;

namespace AutoMarket.Identity.Application.Admin;

// FR-ADM-02 AC1: Admin yalnız Moderator və Admin rolunu verir və ləğv edir. User rolu hər istifadəçidə qalır
internal static class AssignableRoles
{
    public static IReadOnlyList<string> All { get; } = [Roles.Moderator, Roles.Admin];

    // Kanonik ad qaytarır (böyük-kiçik hərfə həssas deyil), tanınmırsa null
    public static string? Normalize(string? role) =>
        All.FirstOrDefault(candidate => string.Equals(candidate, role, StringComparison.OrdinalIgnoreCase));
}
