using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.ChangePassword;

// FR-AUTH-07. İstifadəçi id-si request-də yoxdur, tokendən gəlir (SEC-AUTHZ-02). Şifrələr dəyişdirilmir (SEC-AUTH-01)
[LogRedact]
internal sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword)
{
    public ChangePasswordCommand ToCommand(Guid userId, string? refreshToken) =>
        new(userId, CurrentPassword!, NewPassword!, refreshToken);
}
