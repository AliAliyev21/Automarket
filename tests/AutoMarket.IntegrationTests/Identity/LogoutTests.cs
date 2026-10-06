using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-AUTH-05, SEC-NET-04
public sealed class LogoutTests(ContainersFixture containers)
{
    [Fact]
    public async Task Logout_CurrentSession_RevokesRefreshTokenAndDeletesCookie()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        using var logout = await AuthApi.LogoutAsync(client, session.RefreshToken);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var cookie = AuthApi.GetRefreshCookieHeader(logout).ShouldNotBeNull();
        cookie.ShouldStartWith($"{AuthApi.RefreshCookieName}=;");
        cookie.ShouldContain("expires=Thu, 01 Jan 1970", Case.Insensitive);

        using var refresh = await AuthApi.RefreshAsync(client, session.RefreshToken);
        refresh.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_OtherSession_StaysActive()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        using var secondLogin = await AuthApi.LoginAsync(client, session.Email);
        var secondRefreshToken = AuthApi.GetRefreshToken(secondLogin)!;

        (await AuthApi.LogoutAsync(client, session.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var refresh = await AuthApi.RefreshAsync(client, secondRefreshToken);
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutCookie_NoContentIdempotent()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        using var logout = await AuthApi.LogoutAsync(client, refreshToken: null);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("https://evil.test")]
    public async Task Logout_MissingOrForeignOrigin_Forbidden(string? origin)
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        using var logout = await AuthApi.LogoutAsync(client, session.RefreshToken, origin);

        logout.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        using var refresh = await AuthApi.RefreshAsync(client, session.RefreshToken);
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK, "CSRF attempt must not revoke the session");
    }

    [Fact]
    public async Task LogoutAll_RevokesEverySession()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        using var secondLogin = await AuthApi.LoginAsync(client, session.Email);
        var secondRefreshToken = AuthApi.GetRefreshToken(secondLogin)!;

        using var logoutAll = await AuthApi.LogoutAllAsync(client, session.AccessToken);

        logoutAll.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        AuthApi.GetRefreshCookieHeader(logoutAll).ShouldNotBeNull().ShouldStartWith($"{AuthApi.RefreshCookieName}=;");
        (await AuthApi.RefreshAsync(client, session.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.RefreshAsync(client, secondRefreshToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var userId = await AuthApi.GetUserIdAsync(client, session.AccessToken);
        await Eventually.WaitAsync(
            () => IdentityDatabase.ReadAuditEventTypesAsync(containers.PostgresConnectionString, userId),
            types => types.Contains("auth.logout_all"),
            TestContext.Current.CancellationToken);
    }
}
