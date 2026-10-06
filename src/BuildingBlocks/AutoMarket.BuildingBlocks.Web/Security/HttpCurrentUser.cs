using AutoMarket.BuildingBlocks.Application;
using Microsoft.AspNetCore.Http;

namespace AutoMarket.BuildingBlocks.Web.Security;

// SEC-AUTHZ-02: istifadəçi id-si yalnız token-dəki sub claim-indən (MapInboundClaims = false, ARCHITECTURE §7.2)
internal sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public const string SubjectClaim = "sub";

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid Id
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User.FindFirst(SubjectClaim)?.Value;
            return Guid.TryParse(subject, out var id)
                ? id
                : throw new InvalidOperationException("The current request has no authenticated user.");
        }
    }
}
