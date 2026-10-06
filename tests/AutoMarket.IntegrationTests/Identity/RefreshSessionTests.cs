using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-AUTH-04, SEC-NET-04
public sealed class RefreshSessionTests(ContainersFixture containers)
{
    [Theory]
    [InlineData(null)]
    [InlineData("https://evil.test")]
    public async Task RefreshSession_MissingOrForeignOrigin_Returns403(string? origin)
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        using var response = await AuthApi.RefreshAsync(client, session.RefreshToken, origin);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await AuthApi.ReadProblemAsync(response)).Code.ShouldBe("FORBIDDEN");

        // Origin yoxlaması tokenə toxunmur: icazəli origin ilə token hələ etibarlıdır
        using var allowed = await AuthApi.RefreshAsync(client, session.RefreshToken);
        allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RefreshSession_NoCookie_Returns401()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        using var response = await AuthApi.RefreshAsync(client, refreshToken: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.ReadProblemAsync(response)).Code.ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public async Task RefreshSession_UnknownToken_Returns401AndDeletesCookie()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        using var response = await AuthApi.RefreshAsync(client, "unknown-refresh-token");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        AuthApi.GetRefreshCookieHeader(response)!.ShouldContain("expires=Thu, 01 Jan 1970", Case.Insensitive);
    }

    [Fact]
    public async Task RefreshSession_AfterSlidingLifetime_Returns401()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        // Q3: 14 gün sliding
        factory.Time.Advance(TimeSpan.FromDays(14) + TimeSpan.FromMinutes(1));
        using var response = await AuthApi.RefreshAsync(client, session.RefreshToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshSession_WithinSlidingLifetime_ExtendsSession()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        factory.Time.Advance(TimeSpan.FromDays(10));
        using var first = await AuthApi.RefreshAsync(client, session.RefreshToken);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Yeni token yeni 14 günlük sliding müddət alır: ilk logindən 20 gün sonra hələ etibarlıdır
        factory.Time.Advance(TimeSpan.FromDays(10));
        using var second = await AuthApi.RefreshAsync(client, AuthApi.GetRefreshToken(first));
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
