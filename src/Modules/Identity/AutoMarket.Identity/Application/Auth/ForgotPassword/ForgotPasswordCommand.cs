using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.ForgotPassword;

[LogRedact]
internal sealed record ForgotPasswordCommand(string Email);
