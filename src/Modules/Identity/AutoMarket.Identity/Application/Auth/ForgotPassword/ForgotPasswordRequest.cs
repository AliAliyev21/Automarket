using AutoMarket.BuildingBlocks.Security;
using AutoMarket.BuildingBlocks.Web.Validation;

namespace AutoMarket.Identity.Application.Auth.ForgotPassword;

[LogRedact]
internal sealed record ForgotPasswordRequest(string? Email) : ISanitizableRequest<ForgotPasswordRequest>
{
    public ForgotPasswordRequest Sanitize() => this with { Email = Email?.Trim() };
}
