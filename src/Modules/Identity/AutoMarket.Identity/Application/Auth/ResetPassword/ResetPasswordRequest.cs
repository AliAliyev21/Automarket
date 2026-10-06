using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.ResetPassword;

// Token body-də göndərilir: URL-də token log-a düşə bilər (SEC-LOG-01). Şifrə dəyişdirilmir (SEC-AUTH-01)
[LogRedact]
internal sealed record ResetPasswordRequest(string? Token, string? NewPassword)
{
    public ResetPasswordCommand ToCommand() => new(Token!, NewPassword!);
}
