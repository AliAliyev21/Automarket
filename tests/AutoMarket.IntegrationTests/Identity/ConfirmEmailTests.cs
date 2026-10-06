using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-AUTH-02
public sealed class ConfirmEmailTests(ContainersFixture containers)
{
    [Fact]
    public async Task ConfirmEmail_UnknownToken_Returns400TokenInvalidOrExpired()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();

        using var response = await AuthApi.ConfirmEmailAsync(client, "not-a-real-token");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(response)).Code.ShouldBe("TOKEN_INVALID_OR_EXPIRED");
    }

    [Fact]
    public async Task ConfirmEmail_TokenUsedTwice_SecondReturns400()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();
        await AuthApi.RegisterAsync(client, email);
        var token = await AuthApi.WaitForConfirmationTokenAsync(email);

        using var first = await AuthApi.ConfirmEmailAsync(client, token);
        using var second = await AuthApi.ConfirmEmailAsync(client, token);

        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        second.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(second)).Code.ShouldBe("TOKEN_INVALID_OR_EXPIRED");
    }

    [Fact]
    public async Task ConfirmEmail_After24Hours_Returns400()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();
        await AuthApi.RegisterAsync(client, email);
        var token = await AuthApi.WaitForConfirmationTokenAsync(email);

        // FR-AUTH-02 AC1: token 24 saat etibarlıdır
        factory.Time.Advance(TimeSpan.FromHours(24) + TimeSpan.FromMinutes(1));
        using var response = await AuthApi.ConfirmEmailAsync(client, token);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AuthApi.ReadProblemAsync(response)).Code.ShouldBe("TOKEN_INVALID_OR_EXPIRED");
    }
}
