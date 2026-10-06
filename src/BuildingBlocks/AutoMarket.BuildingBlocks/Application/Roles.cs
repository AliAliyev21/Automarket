namespace AutoMarket.BuildingBlocks.Application;

// REQUIREMENTS 2.1: rollar kumulyativdir (R-01: Admin ⊃ Moderator ⊃ User). Access token-in role claim-ində yazılır
public static class Roles
{
    public const string User = "User";
    public const string Moderator = "Moderator";
    public const string Admin = "Admin";
}
