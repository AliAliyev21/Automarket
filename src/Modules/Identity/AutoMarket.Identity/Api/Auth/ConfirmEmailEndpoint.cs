using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Auth.ConfirmEmail;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Auth;

// FR-AUTH-02. Token body-də göndərilir: URL-də token log-a düşə bilər (SEC-LOG-01, NFR-LOG)
internal static class ConfirmEmailEndpoint
{
    public static RouteGroupBuilder MapConfirmEmail(this RouteGroupBuilder group)
    {
        group.MapPost("/confirm-email", HandleAsync)
            .WithName("ConfirmEmail")
            .AllowAnonymous()
            .AddEndpointFilter<ValidationFilter<ConfirmEmailRequest>>()
            .Accepts<ConfirmEmailRequest>("application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return group;
    }

    private static async Task<Results<NoContent, ApiProblemResult>> HandleAsync(
        ConfirmEmailRequest request,
        ICommandHandler<ConfirmEmailCommand> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ConfirmEmailCommand(request.Token!), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
