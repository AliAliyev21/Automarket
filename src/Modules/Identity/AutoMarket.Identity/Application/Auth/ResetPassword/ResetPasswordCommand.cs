using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.ResetPassword;

[LogRedact]
internal sealed record ResetPasswordCommand(string Token, string NewPassword);
