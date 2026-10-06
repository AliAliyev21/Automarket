using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace AutoMarket.BuildingBlocks.Web.Endpoints;

// NFR-VER: bütün endpoint-lər /api/v1 altındadır. Asp.Versioning qoşulanda route-lar dəyişmir (ARCHITECTURE §8.8)
public static class ApiRoutes
{
    public const string V1Prefix = "/api/v1";

    public static RouteGroupBuilder MapApiV1(this IEndpointRouteBuilder endpoints) => endpoints.MapGroup(V1Prefix);
}
