using AutoMarket.BuildingBlocks.Security;
using AutoMarket.BuildingBlocks.Web.Validation;

namespace AutoMarket.Identity.Application.Auth.Register;

// FR-AUTH-01 AC1. SEC-INP-03: rol, id, status kimi sahələr modeldə yoxdur, göndərilsə nəzərə alınmır
[LogRedact]
internal sealed record RegisterRequest(
    string? Email,
    string? Password,
    string? Name,
    string? Phone,
    bool? TermsAccepted) : ISanitizableRequest<RegisterRequest>
{
    // SEC-INP-05: şifrə dəyişdirilmir (boşluq və istənilən Unicode simvolu icazəlidir, SEC-AUTH-01)
    public RegisterRequest Sanitize() => this with
    {
        Email = Email?.Trim(),
        Name = TextSanitizer.Sanitize(Name),
        Phone = Phone?.Trim(),
    };

    public RegisterCommand ToCommand() => new(Email!, Password!, Name!, string.IsNullOrEmpty(Phone) ? null : Phone);
}
