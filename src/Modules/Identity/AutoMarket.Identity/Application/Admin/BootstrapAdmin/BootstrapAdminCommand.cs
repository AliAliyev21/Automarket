using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Admin.BootstrapAdmin;

[LogRedact]
internal sealed record BootstrapAdminCommand(string Email, string Password, string Name);

internal enum BootstrapAdminOutcome
{
    // Təsdiqlənmiş yeni hesab yaradıldı
    Created,

    // Mövcud aktiv hesaba Admin rolu verildi (şifrə dəyişmədi)
    Promoted,
}
