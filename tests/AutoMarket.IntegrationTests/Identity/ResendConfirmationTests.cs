using System.Net;
using AutoMarket.IntegrationTests.Infrastructure;

namespace AutoMarket.IntegrationTests.Identity;

// FR-AUTH-02 AC3, SEC-AUTH-08
public sealed class ResendConfirmationTests(ContainersFixture containers)
{
    [Fact]
    public async Task ResendConfirmation_NewToken_OldTokenInvalidated()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var email = AuthApi.NewEmail();
        await AuthApi.RegisterAsync(client, email);
        var firstToken = await AuthApi.WaitForConfirmationTokenAsync(email);

        using (var resend = await AuthApi.ResendConfirmationAsync(client, email))
        {
            resend.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        }

        var secondToken = await AuthApi.WaitForConfirmationTokenAsync(email, expectedEmails: 2);
        secondToken.ShouldNotBe(firstToken);

        using (var old = await AuthApi.ConfirmEmailAsync(client, firstToken))
        {
            old.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        using var current = await AuthApi.ConfirmEmailAsync(client, secondToken);
        current.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ResendConfirmation_UnknownEmail_SameResponseAsExisting()
    {
        await using var factory = containers.CreateFactory();
        using var client = factory.CreateApiClient();
        var existing = AuthApi.NewEmail();
        await AuthApi.RegisterAsync(client, existing);

        using var known = await AuthApi.ResendConfirmationAsync(client, existing);
        using var unknown = await AuthApi.ResendConfirmationAsync(client, AuthApi.NewEmail());

        unknown.StatusCode.ShouldBe(known.StatusCode);
        (await unknown.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldBe(await known.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
