using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.Security;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.ChangePassword;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-07 (icazə matrisi sətir 4). Route /auth altındadır ki, refresh cookie (Path=/api/v1/auth) gəlsin və cari sessiya
// saxlanılsın. Kimlik access token-dən (SEC-AUTHZ-02); cookie yalnız cari sessiyanı müəyyən edir
internal static class ChangePasswordEndpoint
{
    public static RouteGroupBuilder MapChangePassword(this RouteGroupBuilder group)
    {
        group.MapPost("/change-password", HandleAsync)
            .WithName("ChangePassword")
            .RequireAuthorization(Policies.User)
            .AddEndpointFilter<ValidationFilter<ChangePasswordRequest>>()
            .Accepts<ChangePasswordRequest>("application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return group;
    }

    private static async Task<Results<NoContent, ApiProblemResult>> HandleAsync(
        ChangePasswordRequest request,
        HttpRequest httpRequest,
        ICurrentUser currentUser,
        ICommandHandler<ChangePasswordCommand> handler,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(currentUser.Id, RefreshTokenCookie.Read(httpRequest));
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
