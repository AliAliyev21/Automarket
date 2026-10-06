using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.ConfirmEmail;

[LogRedact]
internal sealed record ConfirmEmailCommand(string Token);
