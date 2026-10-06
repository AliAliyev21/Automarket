using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.BuildingBlocks.Web.Security;

// SEC-AUTH-06 (ARCHITECTURE §7.1, addım 10): Authentication-dan sonra, Authorization-dan əvvəl. Bloklanmış istifadəçinin
// access tokeni ömrü bitənə qədər də qəbul edilmir. Status cache-dədir (L1 5 s), ona görə gecikmə L1 TTL qədərdir
internal sealed class UserStatusMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var subject = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(HttpCurrentUser.SubjectClaim)?.Value
            : null;

        if (subject is null)
        {
            await next(context);
            return;
        }

        var status = Guid.TryParse(subject, out var userId)
            ? await context.RequestServices.GetRequiredService<IUserStatusReader>().GetStatusAsync(userId, context.RequestAborted)
            : UserAccessStatus.Inactive;

        switch (status)
        {
            case UserAccessStatus.Active:
                await next(context);
                break;
            case UserAccessStatus.Blocked:
                await CommonErrors.AccountBlocked.ToProblem().ExecuteAsync(context);
                break;
            default:
                await CommonErrors.Unauthorized.ToProblem().ExecuteAsync(context);
                break;
        }
    }
}

public static class UserStatusMiddlewareExtensions
{
    public static IApplicationBuilder UseUserStatusCheck(this IApplicationBuilder app) => app.UseMiddleware<UserStatusMiddleware>();
}
