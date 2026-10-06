using AutoMarket.BuildingBlocks.Security;
using AutoMarket.BuildingBlocks.Web.Validation;

namespace AutoMarket.Identity.Application.Auth.Login;

[LogRedact]
internal sealed record LoginRequest(string? Email, string? Password) : ISanitizableRequest<LoginRequest>
{
    public LoginRequest Sanitize() => this with { Email = Email?.Trim() };
}
