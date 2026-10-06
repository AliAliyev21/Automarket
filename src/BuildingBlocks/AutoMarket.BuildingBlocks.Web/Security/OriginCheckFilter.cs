using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AutoMarket.BuildingBlocks.Web.Security;

// SEC-NET-04: cookie qəbul edən endpoint-lər (refresh, logout) SameSite=Strict-dən əlavə Origin ilə CSRF-ə qarşı qorunur.
// Origin yoxdursa və ya CORS siyahısında deyilsə 403 FORBIDDEN
public sealed class OriginCheckFilter(IOptions<AllowedOriginsOptions> options) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var origin = context.HttpContext.Request.Headers.Origin.ToString();
        if (!options.Value.IsAllowed(origin))
        {
            return CommonErrors.Forbidden.ToProblem();
        }

        return await next(context);
    }
}
