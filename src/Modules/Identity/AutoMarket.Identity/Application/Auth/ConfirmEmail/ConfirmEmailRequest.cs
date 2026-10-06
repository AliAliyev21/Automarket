using AutoMarket.BuildingBlocks.Security;

namespace AutoMarket.Identity.Application.Auth.ConfirmEmail;

[LogRedact]
internal sealed record ConfirmEmailRequest(string? Token);
