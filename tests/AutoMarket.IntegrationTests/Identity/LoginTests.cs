using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-AUTH-03, SEC-AUTH-03, SEC-AUTH-05, SEC-AUTH-08
public sealed class LoginTests(ContainersFixture containers)
{
    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_IdenticalResponses()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        using var unknown = await AuthApi.LoginAsync(client, AuthApi.NewEmail());
        using var wrongPassword = await AuthApi.LoginAsync(client, session.Email, "Wrong-Password-123");

        // FR-AUTH-03 AC2: status, kod və mesaj eynidir
        unknown.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        wrongPassword.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        var unknownProblem = await AuthApi.ReadProblemAsync(unknown);
        var wrongPasswordProblem = await AuthApi.ReadProblemAsync(wrongPassword);
        unknownProblem.Code.ShouldBe("INVALID_CREDENTIALS");
        wrongPasswordProblem.Code.ShouldBe(unknownProblem.Code);
        wrongPasswordProblem.Message.ShouldBe(unknownProblem.Message);
        AuthApi.GetRefreshCookieHeader(unknown).ShouldBeNull();
    }

    [Fact]
    public async Task Login_UnconfirmedEmail_EmailNotConfirmedOnlyWithCorrectPassword()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();
        await AuthApi.RegisterAsync(client, email);

        using var correct = await AuthApi.LoginAsync(client, email);
        using var wrong = await AuthApi.LoginAsync(client, email, "Wrong-Password-123");

        correct.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await AuthApi.ReadProblemAsync(correct)).Code.ShouldBe("EMAIL_NOT_CONFIRMED");
        (await AuthApi.ReadProblemAsync(wrong)).Code.ShouldBe("INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_FiveWrongPasswords_LockedOutAndNotified()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);

        for (var i = 0; i < 5; i++)
        {
            using var failed = await AuthApi.LoginAsync(client, session.Email, "Wrong-Password-123");
            (await AuthApi.ReadProblemAsync(failed)).Code.ShouldBe("INVALID_CREDENTIALS");
        }

        using var locked = await AuthApi.LoginAsync(client, session.Email);
        using var lockedWrong = await AuthApi.LoginAsync(client, session.Email, "Wrong-Password-123");

        // FR-AUTH-03 AC5: kilidin bitmə vaxtı göstərilmir; yanlış şifrə ilə kilid açıqlanmır
        locked.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var problem = await AuthApi.ReadProblemAsync(locked);
        problem.Code.ShouldBe("ACCOUNT_LOCKED_OUT");
        problem.Message.ShouldNotContain(":");
        (await AuthApi.ReadProblemAsync(lockedWrong)).Code.ShouldBe("INVALID_CREDENTIALS");

        // SEC-AUTH-03: lockout email ilə bildirilir (təsdiq + lockout = 2 məktub)
        var emails = await FakeEmailTransport.WaitForAsync(session.Email, 2, TestContext.Current.CancellationToken);
        emails.ShouldContain(message => message.Subject.Contains("temporarily locked", StringComparison.Ordinal));

        // 15 dəqiqə sonra kilid açılır
        factory.Time.Advance(TimeSpan.FromMinutes(16));
        using var afterLockout = await AuthApi.LoginAsync(client, session.Email);
        afterLockout.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ElevenSessions_OldestSessionRevoked()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var first = await AuthApi.RegisterConfirmAndLoginAsync(client);

        var latest = string.Empty;
        for (var i = 0; i < 10; i++)
        {
            using var login = await AuthApi.LoginAsync(client, first.Email);
            latest = AuthApi.GetRefreshToken(login)!;
        }

        // SEC-AUTH-05: ən çox 10 aktiv sessiya, ən köhnəsi avtomatik ləğv olunur
        using var oldest = await AuthApi.RefreshAsync(client, first.RefreshToken);
        using var newest = await AuthApi.RefreshAsync(client, latest);

        oldest.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        newest.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_MissingFields_ValidationFailed()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        using var response = await AuthApi.LoginAsync(client, string.Empty, string.Empty);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await AuthApi.ReadProblemAsync(response);
        problem.Code.ShouldBe("VALIDATION_FAILED");
        problem.Errors!.Value.TryGetProperty("email", out _).ShouldBeTrue();
        problem.Errors!.Value.TryGetProperty("password", out _).ShouldBeTrue();
    }
}
