using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutoMarket.BuildingBlocks.Web.Security;
using AutoMarket.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMarket.IntegrationTests.Authorization;

// SEC-AUTHZ-01, NFR-TEST-03 (ARCHITECTURE §10.3): hər endpoint matrisdədir və açıq policy və ya AllowAnonymous-a malikdir
public sealed class EndpointCoverageTests(ContainersFixture containers)
{
    [Fact]
    public async Task Endpoints_All_RegisteredInMatrixWithExplicitAuthorization()
    {
        await using var factory = containers.CreateFactory();
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

        endpoints.ShouldNotBeEmpty();
        foreach (var endpoint in endpoints)
        {
            var route = "/" + endpoint.RoutePattern.RawText!.TrimStart('/');
            var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["*"];

            foreach (var method in methods)
            {
                var inMatrix = AuthorizationMatrix.Endpoints.Any(entry => entry.Method == method && entry.Route == route);
                var isInfrastructure = AuthorizationMatrix.Infrastructure.Contains((method, route));
                (inMatrix || isInfrastructure).ShouldBeTrue($"{method} {route} must be added to AuthorizationMatrix");
            }

            var isAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            var hasPolicy = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(data => !string.IsNullOrEmpty(data.Policy));
            (isAnonymous || hasPolicy).ShouldBeTrue($"{route} must declare a policy or AllowAnonymous");
        }
    }

    [Fact]
    public async Task Endpoints_MatrixAccess_MatchesEndpointMetadata()
    {
        await using var factory = containers.CreateFactory();
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

        foreach (var entry in AuthorizationMatrix.Endpoints)
        {
            var endpoint = endpoints.Single(candidate =>
                "/" + candidate.RoutePattern.RawText!.TrimStart('/') == entry.Route
                && candidate.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains(entry.Method));

            if (entry.Access == Access.Anonymous)
            {
                endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldNotBeNull($"{entry.Route} is public in REQUIREMENTS 2.2");
            }
            else
            {
                var policy = entry.Access == Access.Admin ? Policies.Admin : Policies.User;
                endpoint.Metadata.GetMetadata<IAllowAnonymous>().ShouldBeNull();
                endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().ShouldContain(data => data.Policy == policy, $"{entry.Route}");
            }
        }
    }

    [Fact]
    public async Task ProtectedEndpoints_Anonymous_Return401()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        foreach (var entry in AuthorizationMatrix.Endpoints.Where(entry => entry.Access != Access.Anonymous))
        {
            using var request = new HttpRequestMessage(new HttpMethod(entry.Method), AuthorizationMatrix.ToConcreteRoute(entry.Route));
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{entry.Method} {entry.Route}");
        }
    }

    // SEC-AUTHZ-01/06 (BFLA): Admin endpoint-ləri adi istifadəçiyə 403 qaytarır (body olsa da, handler-ə çatmır)
    [Fact]
    public async Task AdminEndpoints_RegularUser_Return403()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        foreach (var entry in AuthorizationMatrix.Endpoints.Where(entry => entry.Access == Access.Admin))
        {
            using var request = new HttpRequestMessage(new HttpMethod(entry.Method), AuthorizationMatrix.ToConcreteRoute(entry.Route))
            {
                Content = JsonContent.Create(new { reason = "test", role = "Admin" }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, $"{entry.Method} {entry.Route}");
        }
    }
}
