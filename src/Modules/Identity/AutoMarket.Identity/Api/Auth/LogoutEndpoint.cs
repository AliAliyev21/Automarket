using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.Security;
using AutoMarket.Identity.Application.Auth.Logout;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-05 AC1 (icazə matrisi sətir 2). Access token tələb olunmur (vaxtı keçmiş ola bilər), sessiya refresh cookie-dən
// müəyyən edilir. SEC-NET-04: cookie qəbul etdiyi üçün Origin yoxlanılır, cookie hər halda silinir
internal static class LogoutEndpoint
{
    public static RouteGroupBuilder MapLogout(this RouteGroupBuilder group)
    {
        group.MapPost("/logout", HandleAsync)
            .WithName("Logout")
            .AllowAnonymous()
            .AddEndpointFilter<OriginCheckFilter>()
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }

    private static async Task<Results<NoContent, ApiProblemResult>> HandleAsync(
        HttpRequest request,
        HttpResponse response,
        ICommandHandler<LogoutCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LogoutCommand(RefreshTokenCookie.Read(request)), cancellationToken);

        RefreshTokenCookie.Delete(response);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
