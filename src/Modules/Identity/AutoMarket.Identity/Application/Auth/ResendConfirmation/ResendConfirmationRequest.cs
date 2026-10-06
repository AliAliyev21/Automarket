using AutoMarket.BuildingBlocks.Web.Validation;

namespace AutoMarket.Identity.Application.Auth.ResendConfirmation;

internal sealed record ResendConfirmationRequest(string? Email) : ISanitizableRequest<ResendConfirmationRequest>
{
    public ResendConfirmationRequest Sanitize() => this with { Email = Email?.Trim() };
}
