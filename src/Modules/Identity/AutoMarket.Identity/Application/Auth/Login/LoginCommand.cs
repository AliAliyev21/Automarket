using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.Login;

[LogRedact]
internal sealed record LoginCommand(string Email, string Password);
