using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.Register;

[LogRedact]
internal sealed record RegisterCommand(string Email, string Password, string Name, string? Phone);
