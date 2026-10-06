using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.Security;
using AutoMarket.Identity.Application.Auth.LogoutAll;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-05 AC2 "Bütün cihazlardan çıx" (icazə matrisi sətir 2). Kimlik access token-dən (SEC-AUTHZ-02), cookie silinir
internal static class LogoutAllEndpoint
{
    public static RouteGroupBuilder MapLogoutAll(this RouteGroupBuilder group)
    {
        group.MapPost("/logout-all", HandleAsync)
            .WithName("LogoutAll")
            .RequireAuthorization(Policies.User)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }

    private static async Task<Results<NoContent, ApiProblemResult>> HandleAsync(
        HttpResponse response,
        ICurrentUser currentUser,
        ICommandHandler<LogoutAllCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LogoutAllCommand(currentUser.Id), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToProblem();
        }

        RefreshTokenCookie.Delete(response);
        return TypedResults.NoContent();
    }
}
