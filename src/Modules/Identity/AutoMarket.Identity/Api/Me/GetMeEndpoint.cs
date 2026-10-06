using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Endpoints;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.Security;
using AutoMarket.Identity.Application.Me.GetMe;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Me;

// FR-ACC-01 (icazə matrisi sətir 3, yalnız oxuma). İstifadəçi id-si yalnız tokendən (SEC-AUTHZ-02)
internal static class GetMeEndpoint
{
    public static RouteGroupBuilder MapGetMe(this RouteGroupBuilder api)
    {
        api.MapGet("/me", HandleAsync)
            .WithName("GetMe")
            .WithTags("Me")
            .RequireAuthorization(Policies.User)
            .NoStore()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return api;
    }

    private static async Task<Results<Ok<MeResponse>, ApiProblemResult>> HandleAsync(
        ICurrentUser currentUser,
        IQueryHandler<GetMeQuery, MeResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetMeQuery(currentUser.Id), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}
