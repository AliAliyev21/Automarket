using System.Globalization;
using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-AUTH-06, SEC-AUTH-07, SEC-AUTH-08, SEC-RATE-03
public sealed class PasswordResetTests(ContainersFixture containers)
{
    private const string NewPassword = "Rq8$vn4-Brand-New";

    [Fact]
    public async Task ForgotPassword_UnknownEmail_SameResponseAsExistingAndNoEmail()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        var unknown = AuthApi.NewEmail();

        using var existingResponse = await AuthApi.ForgotPasswordAsync(client, session.Email);
        using var unknownResponse = await AuthApi.ForgotPasswordAsync(client, unknown);

        existingResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        unknownResponse.StatusCode.ShouldBe(existingResponse.StatusCode);
        (await unknownResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldBe(await existingResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        await AuthApi.WaitForResetTokenAsync(session.Email);
        FakeEmailTransport.SentTo(unknown).ShouldBeEmpty();
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ChangesPasswordRevokesSessionsAndNotifies()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        (await AuthApi.ForgotPasswordAsync(client, session.Email)).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var token = await AuthApi.WaitForResetTokenAsync(session.Email);

        using var reset = await AuthApi.ResetPasswordAsync(client, token, NewPassword);

        reset.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await AuthApi.RefreshAsync(client, session.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.LoginAsync(client, session.Email)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await AuthApi.LoginAsync(client, session.Email, NewPassword)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // FR-AUTH-06 AC4: "şifrəniz dəyişdirildi" məktubu
        await Eventually.WaitAsync(
            () => Task.FromResult(FakeEmailTransport.SentTo(session.Email)),
            messages => messages.Any(message => message.Subject.Contains("Your password was changed", StringComparison.Ordinal)),
            TestContext.Current.CancellationToken);

        // Birdəfəlik: ikinci istifadə rədd olunur
        using var reuse = await AuthApi.ResetPasswordAsync(client, token, "Another-Pa55word!");
        reuse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(reuse)).Code.ShouldBe("TOKEN_INVALID_OR_EXPIRED");
    }

    [Fact]
    public async Task ResetPassword_AfterOneHour_TokenExpired()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        await AuthApi.ForgotPasswordAsync(client, session.Email);
        var token = await AuthApi.WaitForResetTokenAsync(session.Email);

        factory.Time.Advance(TimeSpan.FromHours(1) + TimeSpan.FromMinutes(1));
        using var reset = await AuthApi.ResetPasswordAsync(client, token, NewPassword);

        reset.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(reset)).Code.ShouldBe("TOKEN_INVALID_OR_EXPIRED");
    }

    [Fact]
    public async Task ForgotPassword_SecondRequest_InvalidatesFirstToken()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        await AuthApi.ForgotPasswordAsync(client, session.Email);
        var first = await AuthApi.WaitForResetTokenAsync(session.Email);
        await AuthApi.ForgotPasswordAsync(client, session.Email);
        var second = await AuthApi.WaitForResetTokenAsync(session.Email, expectedResetEmails: 2);

        (await AuthApi.ResetPasswordAsync(client, first, NewPassword)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ResetPasswordAsync(client, second, NewPassword)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResetPassword_PasswordContainsEmail_ValidationFailedAndTokenStillUsable()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var session = await AuthApi.RegisterConfirmAndLoginAsync(client);
        await AuthApi.ForgotPasswordAsync(client, session.Email);
        var token = await AuthApi.WaitForResetTokenAsync(session.Email);

        using var reset = await AuthApi.ResetPasswordAsync(client, token, session.Email + "-X1");

        reset.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await AuthApi.ReadProblemAsync(reset);
        problem.Code.ShouldBe("VALIDATION_FAILED");
        problem.Errors.ShouldNotBeNull().TryGetProperty("newPassword", out _).ShouldBeTrue();
        (await AuthApi.ResetPasswordAsync(client, token, NewPassword)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ForgotPassword_EmailLimitSharedWithResendConfirmation_Returns429()
    {
        await using var factory = containers.CreateFactory(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["RateLimiting:KeyPrefix"] = $"rl-{Guid.CreateVersion7():N}",
            ["RateLimiting:Rules:recovery-email:PermitLimit"] = 2.ToString(CultureInfo.InvariantCulture),
        });
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();

        (await AuthApi.ForgotPasswordAsync(client, email)).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        (await AuthApi.ResendConfirmationAsync(client, email)).StatusCode.ShouldBe(HttpStatusCode.Accepted);
        using var limited = await AuthApi.ForgotPasswordAsync(client, email);

        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.Contains("Retry-After").ShouldBeTrue();
    }
}
