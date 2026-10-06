using AutoMarket.BuildingBlocks.Application;
using AutoMarket.BuildingBlocks.Web.Endpoints;
using AutoMarket.BuildingBlocks.Web.Errors;
using AutoMarket.BuildingBlocks.Web.Security;
using AutoMarket.BuildingBlocks.Web.Validation;
using AutoMarket.Identity.Application.Admin;
using AutoMarket.Identity.Application.Admin.Users.BlockUser;
using AutoMarket.Identity.Application.Admin.Users.GrantRole;
using AutoMarket.Identity.Application.Admin.Users.RevokeRole;
using AutoMarket.Identity.Application.Admin.Users.UnblockUser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.Identity.Api.Admin;

// /api/v1/admin/users: icazə matrisi sətir 28 (bloklama) və 29 (rollar) — yalnız Admin (SEC-AUTHZ-01, rol yoxlaması
// URL-ə deyil, policy-yə əsaslanır). İcraçı tokendən, hədəf route-dan gəlir. Mövcud olmayan istifadəçi → USER_NOT_FOUND
internal static class AdminUserEndpoints
{
    public static RouteGroupBuilder MapAdminUserEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/admin/users").WithTags("Admin").RequireAuthorization(Policies.Admin).NoStore();

        group.MapPost("/{id:guid}/block", BlockAsync)
            .WithName("BlockUser")
            .AddEndpointFilter<ValidationFilter<BlockUserRequest>>()
            .Accepts<BlockUserRequest>("application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/unblock", UnblockAsync)
            .WithName("UnblockUser")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/roles", GrantRoleAsync)
            .WithName("GrantRole")
            .AddEndpointFilter<ValidationFilter<GrantRoleRequest>>()
            .Accepts<GrantRoleRequest>("application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/roles/{role}", RevokeRoleAsync)
            .WithName("RevokeRole")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<NoContent, ApiProblemResult>> BlockAsync(
        Guid id,
        BlockUserRequest request,
        ICurrentUser currentUser,
        ICommandHandler<BlockUserCommand> handler,
        CancellationToken cancellationToken) =>
        ToResponse(await handler.HandleAsync(request.ToCommand(currentUser.Id, id), cancellationToken));

    private static async Task<Results<NoContent, ApiProblemResult>> UnblockAsync(
        Guid id,
        ICurrentUser currentUser,
        ICommandHandler<UnblockUserCommand> handler,
        CancellationToken cancellationToken) =>
        ToResponse(await handler.HandleAsync(new UnblockUserCommand(currentUser.Id, id), cancellationToken));

    private static async Task<Results<NoContent, ApiProblemResult>> GrantRoleAsync(
        Guid id,
        GrantRoleRequest request,
        ICurrentUser currentUser,
        ICommandHandler<GrantRoleCommand> handler,
        CancellationToken cancellationToken) =>
        ToResponse(await handler.HandleAsync(request.ToCommand(currentUser.Id, id), cancellationToken));

    // Body yoxdur: rol route-dan gəlir və allow-list ilə yoxlanılır (SEC-INP-01)
    private static async Task<Results<NoContent, ApiProblemResult>> RevokeRoleAsync(
        Guid id,
        string role,
        ICurrentUser currentUser,
        ICommandHandler<RevokeRoleCommand> handler,
        CancellationToken cancellationToken)
    {
        if (AssignableRoles.Normalize(role) is not { } assignable)
        {
            return CommonErrors
                .ValidationFailedFor("role", [new FieldError(ValidationCodes.InvalidFormat, $"Role must be one of: {string.Join(", ", AssignableRoles.All)}.")])
                .ToProblem();
        }

        return ToResponse(await handler.HandleAsync(new RevokeRoleCommand(currentUser.Id, id, assignable), cancellationToken));
    }

    private static Results<NoContent, ApiProblemResult> ToResponse(Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
}
