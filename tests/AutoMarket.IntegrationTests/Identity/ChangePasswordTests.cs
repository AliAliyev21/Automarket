using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-AUTH-07, SEC-AUTH-03
public sealed class ChangePasswordTests(ContainersFixture containers)
{
    private const string NewPassword = "Rq8$vn4-Changed-Pass";

    [Fact]
    public async Task ChangePassword_WithSessionCookie_KeepsCurrentSessionRevokesOthers()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        using var otherLogin = await AuthApi.LoginAsync(client, session.Email);
        var otherRefreshToken = AuthApi.GetRefreshToken(otherLogin)!;

        using var change = await AuthApi.ChangePasswordAsync(client, session.AccessToken, AuthApi.Password, NewPassword, session.RefreshToken);

        change.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await AuthApi.RefreshAsync(client, session.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await AuthApi.RefreshAsync(client, otherRefreshToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.LoginAsync(client, session.Email, NewPassword)).StatusCode.ShouldBe(HttpStatusCode.OK);

        await Eventually.WaitAsync(
            () => Task.FromResult(FakeEmailTransport.SentTo(session.Email)),
            messages => messages.Any(message => message.Subject.Contains("Your password was changed", StringComparison.Ordinal)),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_InvalidCredentialsAndPasswordUnchanged()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        using var change = await AuthApi.ChangePasswordAsync(client, session.AccessToken, "Wrong-Password-123", NewPassword);

        change.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.ReadProblemAsync(change)).Code.ShouldBe("INVALID_CREDENTIALS");
        (await AuthApi.LoginAsync(client, session.Email)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_RepeatedWrongCurrentPassword_LocksAccount()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        for (var i = 0; i < 5; i++)
        {
            (await AuthApi.ChangePasswordAsync(client, session.AccessToken, "Wrong-Password-123", NewPassword))
                .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var locked = await AuthApi.ChangePasswordAsync(client, session.AccessToken, AuthApi.Password, NewPassword);
        locked.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await AuthApi.ReadProblemAsync(locked)).Code.ShouldBe("ACCOUNT_LOCKED_OUT");
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_ValidationFailed()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        using var change = await AuthApi.ChangePasswordAsync(client, session.AccessToken, AuthApi.Password, "short");

        change.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(change)).Errors.ShouldNotBeNull().TryGetProperty("newPassword", out _).ShouldBeTrue();
    }
}
