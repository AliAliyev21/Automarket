using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// Tam axın (ARCHITECTURE §6.1): qeydiyyat → təsdiq → login → /me → refresh → reuse → bütün sessiyaların ləğvi
public sealed class AuthFlowTests(ContainersFixture containers)
{
    [Fact]
    public async Task AuthFlow_RegisterConfirmLoginRefreshReuse_RevokesAllSessions()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();

        // Qeydiyyat və təsdiq (FR-AUTH-01, FR-AUTH-02)
        using (var register = await AuthApi.RegisterAsync(client, email))
        {
            register.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        }

        var confirmationToken = await AuthApi.WaitForConfirmationTokenAsync(email);
        using (var confirm = await AuthApi.ConfirmEmailAsync(client, confirmationToken))
        {
            confirm.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Login: access token body-də, refresh token yalnız HttpOnly cookie-də (FR-AUTH-03 AC1, SEC-NET-04)
        using var login = await AuthApi.LoginAsync(client, email);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        var loginBody = await login.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        loginBody.ShouldNotContain("refresh", Case.Insensitive);
        login.Headers.CacheControl!.NoStore.ShouldBeTrue();

        var cookie = AuthApi.GetRefreshCookieHeader(login)!;
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain("secure", Case.Insensitive);
        cookie.ShouldContain("samesite=strict", Case.Insensitive);
        cookie.ShouldContain("path=/api/v1/auth", Case.Insensitive);
        cookie.ShouldContain("max-age=", Case.Insensitive);
        cookie.ShouldNotContain("domain=", Case.Insensitive);

        var accessToken = await AuthApi.ReadAccessTokenAsync(login);
        var firstRefreshToken = AuthApi.GetRefreshToken(login)!;

        using (var me = await AuthApi.GetMeAsync(client, accessToken))
        {
            me.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        // İkinci cihazda ayrıca sessiya
        using var secondLogin = await AuthApi.LoginAsync(client, email);
        var secondDeviceRefreshToken = AuthApi.GetRefreshToken(secondLogin)!;

        // Rotation (FR-AUTH-04 AC1)
        using var refresh = await AuthApi.RefreshAsync(client, firstRefreshToken);
        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rotatedRefreshToken = AuthApi.GetRefreshToken(refresh)!;
        rotatedRefreshToken.ShouldNotBe(firstRefreshToken);
        (await AuthApi.ReadAccessTokenAsync(refresh)).ShouldNotBeNullOrEmpty();

        // Reuse: köhnə token yenidən göndərilir → bütün sessiyalar ləğv olunur, cookie silinir (FR-AUTH-04 AC2)
        using var reuse = await AuthApi.RefreshAsync(client, firstRefreshToken);
        reuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.ReadProblemAsync(reuse)).Code.ShouldBe("REFRESH_TOKEN_REUSED");
        AuthApi.GetRefreshCookieHeader(reuse)!.ShouldContain("expires=Thu, 01 Jan 1970", Case.Insensitive);

        using (var rotatedAfterReuse = await AuthApi.RefreshAsync(client, rotatedRefreshToken))
        {
            rotatedAfterReuse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
            (await AuthApi.ReadProblemAsync(rotatedAfterReuse)).Code.ShouldBe("UNAUTHORIZED");
        }

        using (var secondDevice = await AuthApi.RefreshAsync(client, secondDeviceRefreshToken))
        {
            secondDevice.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}
